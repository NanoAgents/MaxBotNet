// 📁 WebhookEndpointTests.cs - Tests for opt-in webhook endpoint, webhook configuration and polling marker
// 🎯 Core function: Guards issues #35 (no implicit endpoint), #36 (optional webhook URL) and the X-Max-Bot-Api-Secret header.
// 🔗 Key dependencies: MapMaxWebhook, WebhookController, MaxClient, UpdatePoller.
// 💡 Usage: Exercises the endpoint request delegate directly without a test server.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Max.Bot.Api;
using Max.Bot.Configuration;
using Max.Bot.Networking;
using Max.Bot.Polling;
using Max.Bot.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Max.Bot.Tests.Unit.Polling;

public class WebhookEndpointTests
{
    private const string UpdateJson = """{"update_type":"message_created","timestamp":1,"message":{"recipient":{"chat_id":1,"chat_type":"dialog"},"body":{"mid":"mid.1","text":"hi"}}}""";

    [Fact]
    public void Issue35_WebhookController_ShouldNotBeDiscoveredAutomatically()
    {
        typeof(WebhookController).IsAbstract.Should().BeTrue();

        var feature = new ControllerFeature();
        var partManager = new Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager();
        partManager.ApplicationParts.Add(new Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart(typeof(WebhookController).Assembly));
        partManager.FeatureProviders.Add(new ControllerFeatureProvider());
        partManager.PopulateFeature(feature);

        feature.Controllers.Should().BeEmpty();
    }

    [Fact]
    public void WebhookOptions_ShouldUseMaxSecretHeaderByDefault()
    {
        new MaxWebhookOptions().SignatureHeaderName.Should().Be("X-Max-Bot-Api-Secret");
    }

    [Fact]
    public async Task MapMaxWebhook_ShouldDispatchUpdate_WhenSecretMatches()
    {
        var (endpoint, pipeline, handler, services) = CreateEndpoint(secret: "s3cret_token");
        var context = CreateContext(services, UpdateJson);
        context.Request.Headers["X-Max-Bot-Api-Secret"] = "s3cret_token";

        await endpoint(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        pipeline.Verify(p => p.ProcessWebhookAsync(
            It.Is<Update>(u => u.Message!.Mid == "mid.1"),
            handler.Object,
            It.IsAny<IServiceProvider?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MapMaxWebhook_ShouldReturnUnauthorized_WhenSecretMissing()
    {
        var (endpoint, pipeline, _, services) = CreateEndpoint(secret: "s3cret_token");
        var context = CreateContext(services, UpdateJson);

        await endpoint(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        pipeline.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MapMaxWebhook_ShouldReturnBadRequest_WhenBodyIsInvalid()
    {
        var (endpoint, pipeline, _, services) = CreateEndpoint(secret: null);
        var context = CreateContext(services, "not json");

        await endpoint(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        pipeline.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MapMaxWebhook_ShouldReturnPayloadTooLarge_WhenBodyExceedsLimit()
    {
        var (endpoint, pipeline, _, services) = CreateEndpoint(secret: null, maxBodyKilobytes: 1);
        var context = CreateContext(services, "{\"update_type\":\"" + new string('x', 4096) + "\"}", setContentLength: false);

        await endpoint(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
        pipeline.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Issue36_ConfigureWebhookAsync_ShouldUseEndpointAndSecretFromOptions()
    {
        string? sentBody = null;
        var handler = new CapturingHandler(async request =>
        {
            sentBody = request.Content == null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"success":true}""", Encoding.UTF8, "application/json")
            };
        });

        var options = new MaxBotOptions
        {
            Token = "token",
            Webhook = { Endpoint = "https://bot.example.com/api/max/webhook", SecretToken = "s3cret_token" }
        };
        using var client = new MaxClient(options, new HttpClient(handler));

        var response = await client.ConfigureWebhookAsync();

        response.Success.Should().BeTrue();
        using var document = JsonDocument.Parse(sentBody!);
        document.RootElement.GetProperty("url").GetString().Should().Be("https://bot.example.com/api/max/webhook");
        document.RootElement.GetProperty("secret").GetString().Should().Be("s3cret_token");
    }

    [Fact]
    public async Task Issue36_ConfigureWebhookAsync_ShouldRequireUrl_WhenEndpointNotConfigured()
    {
        using var client = new MaxClient(new MaxBotOptions { Token = "token" });

        var act = () => client.ConfigureWebhookAsync();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ConfigureWebhookAsync_ShouldStoreExplicitSecretForValidation()
    {
        var handler = new CapturingHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"success":true}""", Encoding.UTF8, "application/json")
        }));
        var options = new MaxBotOptions { Token = "token" };
        using var client = new MaxClient(options, new HttpClient(handler));

        await client.ConfigureWebhookAsync("https://bot.example.com/hook", secret: "explicit_secret");

        options.Webhook.Endpoint.Should().Be("https://bot.example.com/hook");
        options.Webhook.SecretToken.Should().Be("explicit_secret");
    }

    [Fact]
    public async Task UpdatePoller_ShouldUseMarkerFromResponse()
    {
        var options = new MaxBotOptions { Token = "token" };
        options.Polling.IdleDelay = TimeSpan.FromMilliseconds(10);

        var requests = new List<MaxApiRequest>();
        var secondRequestSeen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pollClient = new Mock<IMaxHttpClient>();
        pollClient
            .Setup(x => x.SendAsyncRaw(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .Returns<MaxApiRequest, CancellationToken>((req, _) =>
            {
                lock (requests)
                {
                    requests.Add(req);
                    if (requests.Count == 1)
                    {
                        return Task.FromResult("{\"updates\":[" + UpdateJson + "],\"marker\":42}");
                    }

                    secondRequestSeen.TrySetResult();
                    return Task.FromResult("{\"updates\":[],\"marker\":42}");
                }
            });

        var poller = new UpdatePoller(Mock.Of<IMaxBotApi>(), Mock.Of<ISubscriptionsApi>(), options, pollClient.Object);
        await poller.StartAsync(Mock.Of<IUpdateHandler>(), CancellationToken.None);
        await secondRequestSeen.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await poller.StopAsync(CancellationToken.None);

        MaxApiRequest second;
        lock (requests)
        {
            requests[0].QueryParameters.Should().NotContainKey("marker");
            second = requests[1];
        }

        second.QueryParameters.Should().ContainKey("marker").WhoseValue.Should().Be("42");
    }

    private static (RequestDelegate Endpoint, Mock<IUpdatePipeline> Pipeline, Mock<IUpdateHandler> Handler, IServiceProvider Services) CreateEndpoint(string? secret, int maxBodyKilobytes = 512)
    {
        var options = new MaxBotOptions { Token = "token" };
        options.Webhook.SecretToken = secret;
        options.Webhook.MaxBodySizeKilobytes = maxBodyKilobytes;

        var pipeline = new Mock<IUpdatePipeline>();
        var handler = new Mock<IUpdateHandler>();

        var services = new ServiceCollection()
            .AddLogging()
            .AddRouting()
            .AddSingleton(options)
            .AddSingleton(pipeline.Object)
            .AddSingleton(handler.Object)
            .BuildServiceProvider();

        var routeBuilder = new TestEndpointRouteBuilder(services);
        routeBuilder.MapMaxWebhook();

        var endpoint = routeBuilder.DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single();

        endpoint.RoutePattern.RawText.Should().Be("/api/max/webhook");
        return (endpoint.RequestDelegate!, pipeline, handler, services);
    }

    private static DefaultHttpContext CreateContext(IServiceProvider services, string body, bool setContentLength = true)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(bytes);
        if (setContentLength)
        {
            context.Request.ContentLength = bytes.Length;
        }

        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class TestEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public TestEndpointRouteBuilder(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;

        public IServiceProvider ServiceProvider { get; }

        public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();

        public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(ServiceProvider);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

        public CapturingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _responder(request);
    }
}
