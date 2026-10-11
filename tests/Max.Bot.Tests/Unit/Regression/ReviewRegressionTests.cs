// 📁 ReviewRegressionTests.cs - Regression tests for problems found in the v0.7.0-alpha review
// 🎯 Core function: Pins culture-invariant ids, webhook hardening, tolerant parsing and upload retries.
// 🔗 Key dependencies: ChatsApi, MessagesApi, FilesApi, MaxHttpClient, MaxClient, MapMaxWebhook, GetUpdatesResponse.
// 💡 Usage: Each test documents the failure scenario it guards against.

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Max.Bot.Api;
using Max.Bot.Configuration;
using Max.Bot.Exceptions;
using Max.Bot.Networking;
using Max.Bot.Polling;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Max.Bot.Types.Requests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Max.Bot.Tests.Unit.Regression;

public class ReviewRegressionTests
{
    private const string UpdateJson = """{"update_type":"message_created","timestamp":1,"message":{"recipient":{"chat_id":1,"chat_type":"dialog"},"body":{"mid":"mid.1","text":"hi"}}}""";

    private readonly Mock<IMaxHttpClient> _http = new();
    private readonly MaxBotOptions _options = new() { Token = "token" };

    #region Culture-invariant identifiers

    [Fact]
    public async Task NegativeChatIds_ShouldUseAsciiMinus_RegardlessOfCurrentCulture()
    {
        var requests = new List<MaxApiRequest>();
        _http.Setup(x => x.SendAsyncRaw(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .Callback<MaxApiRequest, CancellationToken>((req, _) => requests.Add(req))
            .ReturnsAsync("""{"chat_id":-1001,"type":"chat"}""");
        _http.Setup(x => x.SendAsync<MessageResponse>(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .Callback<MaxApiRequest, CancellationToken>((req, _) => requests.Add(req))
            .ReturnsAsync(new MessageResponse { Message = new Message() });

        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "−"; // as in sv-SE, nb-NO, fi-FI
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;

            await new ChatsApi(_http.Object, _options).GetChatAsync(-1001);
            await new MessagesApi(_http.Object, _options).SendMessageAsync(new SendMessageRequest { Text = "hi" }, chatId: -1001);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        requests[0].Endpoint.Should().Be("/chats/-1001");
        requests[1].QueryParameters!["chat_id"].Should().Be("-1001");
    }

    #endregion

    #region Webhook configuration and endpoint

    [Fact]
    public async Task ConfigureWebhookAsync_ShouldReplaceConfiguredSecret_WithExplicitSecret()
    {
        var options = new MaxBotOptions
        {
            Token = "token",
            Webhook = { Endpoint = "https://bot.example.com/hook", SecretToken = "old_secret" }
        };
        using var client = new MaxClient(options, new HttpClient(new StubHandler(_ => Json("""{"success":true}"""))));

        await client.ConfigureWebhookAsync(secret: "new_secret");

        options.Webhook.SecretToken.Should().Be("new_secret");
    }

    [Fact]
    public async Task ConfigureWebhookAsync_ShouldNotChangeOptions_WhenMaxRejectsSubscription()
    {
        var options = new MaxBotOptions { Token = "token", Webhook = { SecretToken = "old_secret" } };
        using var client = new MaxClient(options, new HttpClient(new StubHandler(_ => Json("""{"success":false,"message":"bad url"}"""))));

        var response = await client.ConfigureWebhookAsync("https://bot.example.com/hook", secret: "new_secret");

        response.Success.Should().BeFalse();
        options.Webhook.Endpoint.Should().BeNull();
        options.Webhook.SecretToken.Should().Be("old_secret");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("has space")]
    [InlineData("пароль123")]
    public async Task ConfigureWebhookAsync_ShouldRejectSecretsOutsideMaxFormat(string secret)
    {
        using var client = new MaxClient(new MaxBotOptions { Token = "token" }, new HttpClient(new StubHandler(_ => Json("""{"success":true}"""))));

        var act = () => client.ConfigureWebhookAsync("https://bot.example.com/hook", secret: secret);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void WebhookOptions_Validate_ShouldRejectInvalidSecret()
    {
        var options = new MaxWebhookOptions { SecretToken = "a b" };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentException>().WithParameterName("SecretToken");
    }

    [Fact]
    public async Task MapMaxWebhook_ShouldNotFallBackToUnconfiguredOptions()
    {
        // IOptions<MaxBotOptions> always resolves in ASP.NET Core; an empty instance would disable the secret check.
        var services = new ServiceCollection()
            .AddOptions()
            .AddSingleton(Mock.Of<IUpdatePipeline>())
            .AddSingleton(Mock.Of<IUpdateHandler>())
            .BuildServiceProvider();
        var endpoint = BuildEndpoint(services);
        var context = CreateContext(services, UpdateJson);

        var act = () => endpoint(context);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MapMaxWebhook_ShouldUseMaxClientOptions_WhenOptionsAreNotRegistered()
    {
        var options = new MaxBotOptions { Token = "token", Webhook = { SecretToken = "s3cret_token" } };
        using var client = new MaxClient(options);
        var services = new ServiceCollection()
            .AddOptions()
            .AddSingleton(client)
            .AddSingleton(Mock.Of<IUpdateHandler>())
            .BuildServiceProvider();
        var endpoint = BuildEndpoint(services);
        var context = CreateContext(services, UpdateJson);

        await endpoint(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task MapMaxWebhook_ShouldApplyWebhookHandlerTimeout()
    {
        var options = new MaxBotOptions { Token = "token", Webhook = { HandlerTimeout = TimeSpan.FromMilliseconds(50) } };
        var pipeline = new Mock<IUpdatePipeline>();
        pipeline.Setup(p => p.ProcessWebhookAsync(It.IsAny<Update>(), It.IsAny<IUpdateHandler>(), It.IsAny<IServiceProvider?>(), It.IsAny<CancellationToken>()))
            .Returns<Update, IUpdateHandler, IServiceProvider?, CancellationToken>((_, _, _, ct) => Task.Delay(Timeout.Infinite, ct));
        var services = new ServiceCollection()
            .AddSingleton(options)
            .AddSingleton(pipeline.Object)
            .AddSingleton(Mock.Of<IUpdateHandler>())
            .BuildServiceProvider();
        var endpoint = BuildEndpoint(services);
        var context = CreateContext(services, UpdateJson);

        await endpoint(context).WaitAsync(TimeSpan.FromSeconds(10));

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    #endregion

    #region Tolerant parsing

    [Fact]
    public void FileUploadResult_ShouldNotSerializeTokenDerivedFromPhotos()
    {
        var result = FilesApi.ParseUploadResponse("""{"photos":{"p1":{"token":"photo-token"}}}""");

        result.Token.Should().Be("photo-token");
        MaxJsonSerializer.Serialize(result).Should().Be("""{"photos":{"p1":{"token":"photo-token"}}}""");

        result.Token = "explicit";
        MaxJsonSerializer.Serialize(result).Should().Contain("\"token\":\"explicit\"");
    }

    [Fact]
    public void Attachments_WithUnexpectedFieldTypes_ShouldNotBreakDeserialization()
    {
        var unknown = MaxJsonSerializer.Deserialize<Attachment>("""{"type":"future_media","payload":{"id":"not-a-number","url":["array"]}}""");
        var image = MaxJsonSerializer.Deserialize<Attachment>("""{"type":"image","payload":{"photo_id":1,"token":"t"},"width":"wide"}""");

        unknown.Type.Should().Be("future_media");
        image.Should().BeOfType<PhotoAttachment>().Which.Type.Should().Be("image");
    }

    [Fact]
    public void GetUpdatesResponse_ShouldSkipUpdatesThatCannotBeDeserialized()
    {
        const string body = """{"updates":[{"update_type":"message_created","timestamp":"not-a-number","message":{"timestamp":"bad"}},""" + UpdateJson + """],"marker":42}""";
        var skipped = new List<string>();

        var response = GetUpdatesResponse.ParseTolerant(body, (_, raw) => skipped.Add(raw));

        response.Marker.Should().Be(42);
        response.Updates.Should().ContainSingle().Which.Message!.Mid.Should().Be("mid.1");
        skipped.Should().ContainSingle();
    }

    [Fact]
    public void Update_ShouldSerializeChat_AssignedAfterInference()
    {
        var update = MaxJsonSerializer.Deserialize<Update>(UpdateJson);

        update.Chat = new Chat { ChatId = 5, Type = ChatType.Chat, Title = "Assigned" };
        update.ChatId = 5;

        using var json = JsonDocument.Parse(MaxJsonSerializer.Serialize(update));
        json.RootElement.GetProperty("chat").GetProperty("title").GetString().Should().Be("Assigned");
        json.RootElement.GetProperty("chat_id").GetInt64().Should().Be(5);
    }

    [Fact]
    public void MessageChatCreated_ShouldExposeChatIdFromChatObject()
    {
        var update = MaxJsonSerializer.Deserialize<Update>("""{"update_type":"message_chat_created","timestamp":1,"chat":{"chat_id":-55,"type":"chat","title":"New"},"title":"New","message_id":"mid.btn","start_payload":"ref"}""");

        update.ChatId.Should().Be(-55);
        update.MessageChatCreatedUpdate!.ChatId.Should().Be(-55);
        update.MessageChatCreatedUpdate.StartPayload.Should().Be("ref");
        update.MessageChatCreatedUpdate.MessageId.Should().Be("mid.btn");
    }

    [Fact]
    public void AllowedUsernames_ShouldUseCallbackPresser_NotBotMessageSender()
    {
        var update = MaxJsonSerializer.Deserialize<Update>("""{"update_type":"message_callback","timestamp":1,"callback":{"callback_id":"cb","payload":"x","user":{"user_id":7,"username":"alice"}},"message":{"sender":{"user_id":1,"username":"my_bot","is_bot":true},"recipient":{"chat_id":7,"chat_type":"dialog"},"body":{"mid":"mid.kb"}}}""");
        var options = new MaxBotOptions { Token = "token" };
        options.Handling.AllowedUsernames.Add("alice");

        var shouldDispatch = UpdateFilterUtilities.ShouldDispatch(update, null, UpdateFilterUtilities.BuildAllowedUsernames(options));

        UpdateFilterUtilities.ExtractUsername(update).Should().Be("alice");
        shouldDispatch.Should().BeTrue();
    }

    [Fact]
    public void GroupMembershipEvents_ShouldReportChatType()
    {
        var userAdded = MaxJsonSerializer.Deserialize<Update>("""{"update_type":"user_added","timestamp":1,"chat_id":-10,"user":{"user_id":2},"inviter_id":3,"is_channel":false}""");

        userAdded.ChatType.Should().Be(ChatType.Chat);
    }

    [Fact]
    public void OpenAppAndClipboardButtons_ShouldUseMaxFormat()
    {
        var openApp = new InlineKeyboardButton { Type = ButtonType.OpenApp, Text = "App", WebApp = "my_bot", ContactId = 42, Payload = "start" };
        var clipboard = new InlineKeyboardButton { Type = ButtonType.Clipboard, Text = "Copy", Payload = "PROMO2026" };
        var geo = new InlineKeyboardButton { Type = ButtonType.RequestGeoLocation, Text = "Where", Quick = true };

        using var openAppJson = JsonDocument.Parse(MaxJsonSerializer.Serialize(openApp));
        using var clipboardJson = JsonDocument.Parse(MaxJsonSerializer.Serialize(clipboard));
        using var geoJson = JsonDocument.Parse(MaxJsonSerializer.Serialize(geo));

        openAppJson.RootElement.GetProperty("type").GetString().Should().Be("open_app");
        openAppJson.RootElement.GetProperty("web_app").GetString().Should().Be("my_bot");
        openAppJson.RootElement.GetProperty("contact_id").GetInt64().Should().Be(42);
        clipboardJson.RootElement.GetProperty("type").GetString().Should().Be("clipboard");
        clipboardJson.RootElement.GetProperty("payload").GetString().Should().Be("PROMO2026");
        geoJson.RootElement.GetProperty("type").GetString().Should().Be("request_geo_location");
        geoJson.RootElement.GetProperty("quick").GetBoolean().Should().BeTrue();

        var roundTrip = MaxJsonSerializer.Deserialize<InlineKeyboardButton>(openAppJson.RootElement.GetRawText());
        roundTrip.Type.Should().Be(ButtonType.OpenApp);
        roundTrip.WebApp.Should().Be("my_bot");
        MaxJsonSerializer.Deserialize<InlineKeyboardButton>(clipboardJson.RootElement.GetRawText()).Type.Should().Be(ButtonType.Clipboard);
    }

    [Fact]
    public async Task GetChatMembersPageAsync_ShouldReturnMarker()
    {
        MaxApiRequest? captured = null;
        _http.Setup(x => x.SendAsyncRaw(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .Callback<MaxApiRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync("""{"members":[{"user_id":1,"name":"A"}],"marker":99}""");

        var page = await new ChatsApi(_http.Object, _options).GetChatMembersPageAsync(-10, marker: 5, count: 20);

        page.Members.Should().ContainSingle();
        page.Marker.Should().Be(99);
        captured!.Endpoint.Should().Be("/chats/-10/members");
        captured.QueryParameters!["marker"].Should().Be("5");
        captured.QueryParameters["count"].Should().Be("20");
    }

    #endregion

    #region Retries

    [Fact]
    public async Task SendMessageAsync_WithAttachments_ShouldRetryAttachmentNotReady()
    {
        _http.SetupSequence(x => x.SendAsync<MessageResponse>(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MaxAttachmentNotReadyException())
            .ReturnsAsync(new MessageResponse { Message = new Message { Body = new MessageBody { Mid = "mid.ok" } } });

        var api = new MessagesApi(_http.Object, _options);
        var message = await api.SendMessageAsync(new SendMessageRequest { Attachments = new[] { AttachmentRequest.File("t") } }, chatId: 1);

        message.Mid.Should().Be("mid.ok");
        _http.Verify(x => x.SendAsync<MessageResponse>(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UploadFileDataAsync_ShouldResendWholeNonSeekableStreamOnRetry()
    {
        var uploadedLengths = new List<long>();
        _http.Setup(x => x.SendAsyncRaw(It.IsAny<string>(), It.IsAny<Func<HttpContent?>?>(), It.IsAny<CancellationToken>(), It.IsAny<HttpMethod?>()))
            .Returns<string, Func<HttpContent?>?, CancellationToken, HttpMethod?>(async (_, factory, _, _) =>
            {
                for (var i = 0; i < 2; i++)
                {
                    using var content = factory!();
                    uploadedLengths.Add((await content!.ReadAsByteArrayAsync()).Length);
                }

                return """{"token":"t"}""";
            });

        var api = new FilesApi(_http.Object, _options);
        using var stream = new NonSeekableStream(Enumerable.Repeat((byte)1, 5000).ToArray());
        var result = await api.UploadFileDataAsync("https://upload", stream, "file.bin");

        result.Token.Should().Be("t");
        uploadedLengths.Should().HaveCount(2);
        uploadedLengths[0].Should().BeGreaterThan(5000);
        uploadedLengths[1].Should().Be(uploadedLengths[0]);
    }

    [Fact]
    public async Task UploadFileResumableAsync_ShouldNotIssueTrailingGet_WhenCdnAcknowledgedChunks()
    {
        _http.Setup(x => x.SendAsyncRaw(It.IsAny<string>(), It.IsAny<Func<HttpContent?>?>(), It.IsAny<CancellationToken>(), It.IsAny<HttpMethod?>()))
            .ReturnsAsync("<retval>1</retval>");

        var api = new FilesApi(_http.Object, _options);
        using var stream = new MemoryStream(new byte[10]);
        var result = await api.UploadFileResumableAsync("https://vu.oneme.ru/upload", stream, chunkSize: 4, fileName: "clip.mp4");

        result.Token.Should().BeNull();
        _http.Verify(x => x.SendAsyncRaw(It.IsAny<string>(), null, It.IsAny<CancellationToken>(), HttpMethod.Get), Times.Never);
    }

    [Fact]
    public async Task RetryAfter_InThePast_ShouldNotBreakRetries()
    {
        var calls = 0;
        var handler = new StubHandler(_ =>
        {
            if (calls++ == 0)
            {
                var limited = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("""{"code":"too.many.requests","message":"slow down"}""") };
                limited.Headers.TryAddWithoutValidation("Retry-After", DateTimeOffset.UtcNow.AddMinutes(-5).ToString("R"));
                return limited;
            }

            return Json("""{"ok":true}""");
        });
        using var client = new MaxHttpClient(new HttpClient(handler), new MaxBotClientOptions { BaseUrl = "https://platform-api.max.ru", RetryCount = 1 });

        var body = await client.SendAsyncRaw(new MaxApiRequest { Method = HttpMethod.Get, Endpoint = "/me" });

        body.Should().Be("""{"ok":true}""");
        calls.Should().Be(2);
    }

    #endregion

    private static RequestDelegate BuildEndpoint(IServiceProvider services)
    {
        var builder = new TestEndpointRouteBuilder(services);
        builder.MapMaxWebhook();
        return builder.DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().Single().RequestDelegate!;
    }

    private static DefaultHttpContext CreateContext(IServiceProvider services, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = HttpMethods.Post;
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private sealed class TestEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public TestEndpointRouteBuilder(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;

        public IServiceProvider ServiceProvider { get; }

        public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();

        public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(ServiceProvider);
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(byte[] data)
            : base(data)
        {
        }

        public override bool CanSeek => false;

        public override long Position
        {
            get => base.Position;
            set => throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
    }
}
