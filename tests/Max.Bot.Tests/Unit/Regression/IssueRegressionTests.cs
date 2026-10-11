// 📁 IssueRegressionTests.cs - Regression tests for reported GitHub issues
// 🎯 Core function: Pins the MAX API contracts reported in issues #15-#38 with real payload shapes.
// 🔗 Key dependencies: MaxJsonSerializer, MessagesApi, ChatsApi, FilesApi, MaxHttpClient, MaxClient.
// 💡 Usage: Each test names the issue it guards against.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Max.Bot.Api;
using Max.Bot.Configuration;
using Max.Bot.Exceptions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Max.Bot.Types.Requests;
using Moq;
using Xunit;

namespace Max.Bot.Tests.Unit.Regression;

public class IssueRegressionTests
{
    private readonly Mock<IMaxHttpClient> _http = new();
    private readonly MaxBotOptions _options = new()
    {
        Token = "test-token-123",
        BaseUrl = "https://platform-api.max.ru"
    };

    #region #34 AnswerCallbackQueryAsync uses callback_id

    [Fact]
    public async Task Issue34_AnswerCallbackQueryAsync_ShouldSendCallbackIdQueryParameter()
    {
        MaxApiRequest? captured = null;
        _http.Setup(x => x.SendAsync<Response>(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .Callback<MaxApiRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new Response { Success = true });

        var api = new MessagesApi(_http.Object, _options);
        var result = await api.AnswerCallbackQueryAsync("cb-123", new AnswerCallbackQueryRequest { Notification = "Done" });

        result.Success.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Endpoint.Should().Be("/answers");
        captured.QueryParameters.Should().ContainKey("callback_id").WhoseValue.Should().Be("cb-123");
        captured.QueryParameters.Should().NotContainKey("callback_query_id");
    }

    [Fact]
    public void Issue34_NewMessageBody_ShouldSerializeAttachmentRequestsAndLink()
    {
        var keyboard = new InlineKeyboard(new[]
        {
            new[] { new InlineKeyboardButton { Text = "OK", CallbackData = "ok" } }
        });
        var request = new AnswerCallbackQueryRequest
        {
            Message = new NewMessageBody
            {
                Text = "Updated",
                Attachments = new[] { AttachmentRequest.Keyboard(keyboard) },
                Link = new NewMessageLink { Type = MessageLinkType.Reply, Id = "mid.1" }
            }
        };

        var json = MaxJsonSerializer.Serialize(request);

        using var document = JsonDocument.Parse(json);
        var message = document.RootElement.GetProperty("message");
        var attachment = message.GetProperty("attachments")[0];
        attachment.GetProperty("type").GetString().Should().Be("inline_keyboard");
        attachment.GetProperty("payload").GetProperty("buttons")[0][0].GetProperty("payload").GetString().Should().Be("ok");
        message.GetProperty("link").GetProperty("mid").GetString().Should().Be("mid.1");
        message.GetProperty("link").GetProperty("type").GetString().Should().Be("reply");
    }

    #endregion

    #region #18 GetMessagesAsync parses {"messages": [...]}

    [Fact]
    public async Task Issue18_GetMessagesAsync_ShouldParseMessagesWrapper()
    {
        const string body = """{"messages":[{"recipient":{"chat_id":10,"chat_type":"chat"},"timestamp":1,"body":{"mid":"mid.1","seq":1,"text":"hi"}}]}""";
        MaxApiRequest? captured = null;
        _http.Setup(x => x.SendAsyncRaw(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .Callback<MaxApiRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(body);

        var api = new MessagesApi(_http.Object, _options);
        var messages = await api.GetMessagesAsync(10, count: 20, from: 2000, to: 1000);

        messages.Should().ContainSingle();
        messages[0].Mid.Should().Be("mid.1");
        captured!.QueryParameters.Should().Contain(new KeyValuePair<string, string?>("chat_id", "10"));
        captured.QueryParameters.Should().Contain(new KeyValuePair<string, string?>("count", "20"));
        captured.QueryParameters.Should().Contain(new KeyValuePair<string, string?>("from", "2000"));
        captured.QueryParameters.Should().Contain(new KeyValuePair<string, string?>("to", "1000"));
    }

    [Fact]
    public async Task Issue18_GetMessagesByIdsAsync_ShouldJoinIds()
    {
        MaxApiRequest? captured = null;
        _http.Setup(x => x.SendAsyncRaw(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .Callback<MaxApiRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync("""{"messages":[]}""");

        var api = new MessagesApi(_http.Object, _options);
        var messages = await api.GetMessagesByIdsAsync(new[] { "mid.1", "mid.2" });

        messages.Should().BeEmpty();
        captured!.QueryParameters.Should().Contain(new KeyValuePair<string, string?>("message_ids", "mid.1,mid.2"));
        captured.QueryParameters.Should().NotContainKey("chat_id");
    }

    #endregion

    #region #22 attachment.not.ready

    [Fact]
    public async Task Issue22_MaxHttpClient_ShouldParseFlatErrorBody_AndThrowAttachmentNotReady()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"code":"attachment.not.ready","message":"Key: errors.process.attachment.file.not.processed"}""", Encoding.UTF8, "application/json")
        });
        using var client = new MaxHttpClient(new HttpClient(handler), new MaxBotClientOptions { BaseUrl = "https://platform-api.max.ru", RetryCount = 0 });

        var act = () => client.SendAsyncRaw(new MaxApiRequest { Method = HttpMethod.Post, Endpoint = "/messages", Body = new { } });

        var exception = await act.Should().ThrowAsync<MaxAttachmentNotReadyException>();
        exception.Which.ErrorCode.Should().Be("attachment.not.ready");
        exception.Which.Message.Should().Be("Key: errors.process.attachment.file.not.processed");
        exception.Which.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Issue22_MaxHttpClient_ShouldExposeCodeAndMessage_ForOtherFlatErrors()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"code":"proto.payload","message":"text: size must be between 0 and 4000"}""", Encoding.UTF8, "application/json")
        });
        using var client = new MaxHttpClient(new HttpClient(handler), new MaxBotClientOptions { BaseUrl = "https://platform-api.max.ru", RetryCount = 0 });

        var act = () => client.SendAsyncRaw(new MaxApiRequest { Method = HttpMethod.Post, Endpoint = "/messages", Body = new { } });

        var exception = await act.Should().ThrowAsync<MaxApiException>();
        exception.Which.Should().NotBeOfType<MaxAttachmentNotReadyException>();
        exception.Which.ErrorCode.Should().Be("proto.payload");
        exception.Which.Message.Should().Be("text: size must be between 0 and 4000");
    }

    [Fact]
    public async Task Issue22_SendMessageWithAttachmentAsync_ShouldRetryAttachmentNotReady()
    {
        var expected = new Message { Body = new MessageBody { Mid = "mid.ok" } };
        _http.SetupSequence(x => x.SendAsync<MessageResponse>(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MaxAttachmentNotReadyException("Key: errors.process.attachment.file.not.processed"))
            .ReturnsAsync(new MessageResponse { Message = expected });

        var api = new MessagesApi(_http.Object, _options);
        var result = await api.SendMessageWithAttachmentAsync(AttachmentRequest.Video("video-token"), chatId: 1);

        result.Mid.Should().Be("mid.ok");
        _http.Verify(x => x.SendAsync<MessageResponse>(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Issue22_SendMessageWithAttachmentAsync_ShouldNotRetryOtherBadRequests()
    {
        _http.Setup(x => x.SendAsync<MessageResponse>(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MaxApiException("text: size must be between 0 and 4000", "proto.payload", HttpStatusCode.BadRequest));

        var api = new MessagesApi(_http.Object, _options);
        var act = () => api.SendMessageWithAttachmentAsync(AttachmentRequest.Video("video-token"), chatId: 1);

        await act.Should().ThrowAsync<MaxApiException>().Where(ex => ex.ErrorCode == "proto.payload");
        _http.Verify(x => x.SendAsync<MessageResponse>(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Issue22_EditMessageAsync_ShouldRetryAttachmentNotReady()
    {
        _http.SetupSequence(x => x.SendAsync<Response>(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MaxAttachmentNotReadyException())
            .ReturnsAsync(new Response { Success = true });

        var api = new MessagesApi(_http.Object, _options);
        var result = await api.EditMessageAsync("mid.1", new EditMessageRequest { Attachments = new[] { AttachmentRequest.Image("img-token") } });

        result.Success.Should().BeTrue();
        _http.Verify(x => x.SendAsync<Response>(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region #23 GetChatsAsync parses {"chats": [...], "marker": ...}

    [Fact]
    public async Task Issue23_GetChatsAsync_ShouldParseChatList()
    {
        const string body = """{"chats":[{"chat_id":-1001,"type":"chat","status":"active","title":"Team","is_public":false,"participants_count":3,"messages_count":42}],"marker":777}""";
        _http.Setup(x => x.SendAsyncRaw(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(body);

        var api = new ChatsApi(_http.Object, _options);
        var chats = await api.GetChatsAsync();

        chats.Should().ContainSingle();
        chats[0].ChatId.Should().Be(-1001);
        chats[0].Title.Should().Be("Team");
        chats[0].MessagesCount.Should().Be(42);
    }

    [Fact]
    public async Task Issue23_GetChatListAsync_ShouldPassPaginationAndReturnMarker()
    {
        MaxApiRequest? captured = null;
        _http.Setup(x => x.SendAsyncRaw(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .Callback<MaxApiRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync("""{"chats":[],"marker":null}""");

        var api = new ChatsApi(_http.Object, _options);
        var page = await api.GetChatListAsync(count: 10, marker: 5);

        page.Chats.Should().BeEmpty();
        page.Marker.Should().BeNull();
        captured!.QueryParameters.Should().Contain(new KeyValuePair<string, string?>("count", "10"));
        captured.QueryParameters.Should().Contain(new KeyValuePair<string, string?>("marker", "5"));
    }

    [Fact]
    public void Chat_ShouldTolerateUnknownStatus()
    {
        var suspended = MaxJsonSerializer.Deserialize<Chat>("""{"chat_id":1,"type":"channel","status":"suspended"}""");
        var unknown = MaxJsonSerializer.Deserialize<Chat>("""{"chat_id":1,"type":"channel","status":"something_new"}""");

        suspended.Status.Should().Be(ChatStatus.Suspended);
        unknown.Status.Should().BeNull();
    }

    [Fact]
    public void ChatMember_ShouldSkipUnknownPermissions()
    {
        var member = MaxJsonSerializer.Deserialize<ChatMember>("""{"user_id":1,"is_admin":true,"permissions":["read_all_messages","brand_new_permission","pin_message"]}""");

        member.Permissions.Should().Equal(ChatAdminPermission.ReadAllMessages, ChatAdminPermission.PinMessage);
    }

    [Fact]
    public async Task GetPinnedMessageAsync_ShouldParseMessageWrapper()
    {
        _http.SetupSequence(x => x.SendAsyncRaw(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"message":{"body":{"mid":"mid.pinned","text":"rules"}}}""")
            .ReturnsAsync("""{"message":null}""");

        var api = new ChatsApi(_http.Object, _options);

        var pinned = await api.GetPinnedMessageAsync(1);
        var none = await api.GetPinnedMessageAsync(1);

        pinned!.Mid.Should().Be("mid.pinned");
        none.Should().BeNull();
    }

    [Fact]
    public void AddChatAdminRequest_ShouldSerializeAdminsArray()
    {
        var request = new AddChatAdminRequest
        {
            Admins = new[]
            {
                new ChatAdmin { UserId = 42, Permissions = new[] { ChatAdminPermission.ReadAllMessages, ChatAdminPermission.PinMessage }, Alias = "Mod" }
            }
        };

        var json = MaxJsonSerializer.Serialize(request);

        json.Should().Be("""{"admins":[{"user_id":42,"permissions":["read_all_messages","pin_message"],"alias":"Mod"}]}""");
        request.UserId.Should().Be(42);
    }

    #endregion

    #region #20 #21 #25 incoming attachments

    [Fact]
    public void Issue20_ImageAttachment_ShouldReadPayload()
    {
        const string json = """{"payload":{"photo_id":9290792871,"token":"photo-token","url":"https://i.oneme.ru/i?r=abc"},"type":"image"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        var photo = attachment.Should().BeOfType<PhotoAttachment>().Subject;
        photo.Id.Should().Be(9290792871);
        photo.Token.Should().Be("photo-token");
        photo.FileId.Should().Be("photo-token");
        photo.Url.Should().Be("https://i.oneme.ru/i?r=abc");
    }

    [Fact]
    public void FileAttachment_ShouldMergeRootFilenameAndSize()
    {
        const string json = """{"type":"file","payload":{"url":"https://files/doc.pdf","token":"file-token"},"filename":"doc.pdf","size":2048}""";

        var document = MaxJsonSerializer.Deserialize<Attachment>(json).Should().BeOfType<DocumentAttachment>().Subject;

        document.FileName.Should().Be("doc.pdf");
        document.FileSize.Should().Be(2048);
        document.Token.Should().Be("file-token");
        document.Url.Should().Be("https://files/doc.pdf");
    }

    [Fact]
    public void VideoAttachment_ShouldMergeRootMetadata()
    {
        const string json = """{"type":"video","payload":{"url":"https://video","token":"video-token"},"thumbnail":{"url":"https://thumb"},"width":1280,"height":720,"duration":15}""";

        var video = MaxJsonSerializer.Deserialize<Attachment>(json).Should().BeOfType<VideoAttachment>().Subject;

        video.Token.Should().Be("video-token");
        video.Thumbnail!.Url.Should().Be("https://thumb");
        video.Width.Should().Be(1280);
        video.Height.Should().Be(720);
        video.Duration.Should().Be(15);
    }

    [Fact]
    public void StickerAndShareAttachments_ShouldBeRecognized()
    {
        var sticker = MaxJsonSerializer.Deserialize<Attachment>("""{"type":"sticker","payload":{"url":"https://sticker","code":"abc"},"width":128,"height":128}""");
        var share = MaxJsonSerializer.Deserialize<Attachment>("""{"type":"share","payload":{"url":"https://example.com"},"title":"Example","description":"Desc","image_url":"https://img"}""");

        sticker.Should().BeOfType<StickerAttachment>().Which.Code.Should().Be("abc");
        var preview = share.Should().BeOfType<ShareAttachment>().Subject;
        preview.Url.Should().Be("https://example.com");
        preview.Title.Should().Be("Example");
        preview.ImageUrl.Should().Be("https://img");
    }

    [Fact]
    public void Issue25_ContactAttachment_ShouldReadPayload()
    {
        const string json = """{"type":"contact","payload":{"vcf_info":"BEGIN:VCARD\r\nVERSION:3.0\r\nFN:Ivan Petrov\r\nTEL;TYPE=cell:+79001234567\r\nEND:VCARD","max_info":{"user_id":5,"first_name":"Ivan","last_name":"Petrov","name":"Ivan Petrov"},"hash":"h1"}}""";

        var contact = MaxJsonSerializer.Deserialize<Attachment>(json).Should().BeOfType<ContactAttachment>().Subject;

        contact.MaxInfo!.Id.Should().Be(5);
        contact.Hash.Should().Be("h1");
        contact.PhoneNumber.Should().Be("+79001234567");
        contact.FullName.Should().Be("Ivan Petrov");
    }

    [Fact]
    public async Task Issue21_GetVideoAsync_ShouldReadUrlsObject()
    {
        const string body = """{"token":"video-token","urls":{"mp4_480":"https://v/480","mp4_360":"https://v/360","mp4_240":"https://v/240","mp4_144":"https://v/144"},"thumbnail":{"url":"https://thumb"},"width":640,"height":480,"duration":12}""";
        _http.Setup(x => x.SendAsyncRaw(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(body);

        var api = new MessagesApi(_http.Object, _options);
        var video = await api.GetVideoAsync("video-token");

        video.Urls.Should().ContainKey("mp4_480").WhoseValue.Should().Be("https://v/480");
        video.Thumbnail!.Url.Should().Be("https://thumb");
        video.Duration.Should().Be(12);
    }

    #endregion

    #region #16 #37 updates

    [Fact]
    public void Issue16_BotStarted_ShouldExposeDeepLinkPayload()
    {
        const string json = """{"update_type":"bot_started","timestamp":1,"chat_id":100,"user":{"user_id":5,"name":"Ivan"},"payload":"123","user_locale":"ru"}""";

        var update = MaxJsonSerializer.Deserialize<Update>(json);

        update.Payload.Should().Be("123");
        update.BotStartedUpdate!.Payload.Should().Be("123");
        update.BotStartedUpdate.ChatId.Should().Be(100);
    }

    [Fact]
    public void Issue37_MessageCreated_ShouldExposeChatFromRecipient()
    {
        const string json = """{"update_type":"message_created","timestamp":1,"message":{"sender":{"user_id":5},"recipient":{"chat_id":-700,"chat_type":"chat"},"body":{"mid":"mid.1","text":"hi"}}}""";

        var update = MaxJsonSerializer.Deserialize<Update>(json);

        update.Chat.Should().NotBeNull();
        update.Chat!.ChatId.Should().Be(-700);
        update.Chat.Type.Should().Be(ChatType.Chat);
        update.ChatId.Should().Be(-700);
        update.ChatType.Should().Be(ChatType.Chat);

        // The inferred chat is not written back as if MAX had sent it.
        using var serialized = JsonDocument.Parse(MaxJsonSerializer.Serialize(update));
        serialized.RootElement.TryGetProperty("chat", out _).Should().BeFalse();
        serialized.RootElement.TryGetProperty("chat_id", out _).Should().BeFalse();
    }

    [Fact]
    public void Issue37_DialogMessage_ShouldReportDialogChatType()
    {
        const string json = """{"update_type":"message_created","timestamp":1,"message":{"recipient":{"chat_id":79313411,"chat_type":"dialog","user_id":94399782},"body":{"mid":"mid.1","text":"/start"}}}""";

        var update = MaxJsonSerializer.Deserialize<Update>(json);

        update.ChatType.Should().Be(ChatType.Dialog);
    }

    [Fact]
    public void MessageRemoved_ShouldExposeIdentifiers()
    {
        const string json = """{"update_type":"message_removed","timestamp":1,"message_id":"mid.removed","chat_id":10,"user_id":20}""";

        var update = MaxJsonSerializer.Deserialize<Update>(json);

        var removed = update.MessageRemovedUpdate;
        removed.Should().NotBeNull();
        removed!.MessageId.Should().Be("mid.removed");
        removed.ChatId.Should().Be(10);
        removed.UserId.Should().Be(20);
    }

    [Fact]
    public void ChatEvents_ShouldExposeChatIdAndExtraFields()
    {
        var botAdded = MaxJsonSerializer.Deserialize<Update>("""{"update_type":"bot_added","timestamp":1,"chat_id":10,"user":{"user_id":2},"is_channel":true}""");
        var userRemoved = MaxJsonSerializer.Deserialize<Update>("""{"update_type":"user_removed","timestamp":1,"chat_id":10,"user":{"user_id":3},"admin_id":4,"is_channel":false}""");
        var titleChanged = MaxJsonSerializer.Deserialize<Update>("""{"update_type":"chat_title_changed","timestamp":1,"chat_id":10,"user":{"user_id":2},"title":"New title"}""");

        botAdded.BotAddedUpdate!.ChatId.Should().Be(10);
        botAdded.BotAddedUpdate.IsChannel.Should().BeTrue();
        botAdded.ChatType.Should().Be(ChatType.Channel);
        userRemoved.UserRemovedUpdate!.AdminId.Should().Be(4);
        userRemoved.UserRemovedUpdate.ChatId.Should().Be(10);
        titleChanged.ChatTitleChangedUpdate!.Title.Should().Be("New title");
        titleChanged.ChatTitleChangedUpdate.ChatId.Should().Be(10);
    }

    #endregion

    #region #15 #38 outgoing attachments and uploads

    [Fact]
    public void Issue15_ImageFromUrl_ShouldSerializeUrlPayload()
    {
        var json = MaxJsonSerializer.Serialize(new SendMessageRequest
        {
            Text = "pic",
            Attachments = new[] { AttachmentRequest.ImageFromUrl("https://example.com/cat.jpg") }
        });

        json.Should().Contain("""{"type":"image","payload":{"url":"https://example.com/cat.jpg"}}""");
    }

    [Fact]
    public void AttachmentRequest_Location_ShouldSerializeFlatCoordinates()
    {
        var json = MaxJsonSerializer.Serialize(AttachmentRequest.Location(55.75, 37.62));

        json.Should().Be("""{"type":"location","latitude":55.75,"longitude":37.62}""");
    }

    [Fact]
    public void AttachmentRequest_Contact_ShouldUseSnakeCasePayload()
    {
        using var json = JsonDocument.Parse(MaxJsonSerializer.Serialize(AttachmentRequest.Contact(name: "Ivan", vcfPhone: "+79001234567")));

        json.RootElement.GetProperty("type").GetString().Should().Be("contact");
        var payload = json.RootElement.GetProperty("payload");
        payload.GetProperty("name").GetString().Should().Be("Ivan");
        payload.GetProperty("vcf_phone").GetString().Should().Be("+79001234567");
        payload.TryGetProperty("contact_id", out _).Should().BeFalse();
    }

    [Fact]
    public void Issue38_ImageUploadResponse_ShouldExposePhotosAndToken()
    {
        const string body = """{"photos":{"aBcD123":{"token":"photo-token"}}}""";

        var result = FilesApi.ParseUploadResponse(body);

        result.Photos.Should().ContainKey("aBcD123").WhoseValue.Token.Should().Be("photo-token");
        result.Token.Should().Be("photo-token");

        var attachmentJson = MaxJsonSerializer.Serialize(AttachmentRequest.FromUpload(UploadType.Image, result));
        attachmentJson.Should().Be("""{"type":"image","payload":{"photos":{"aBcD123":{"token":"photo-token"}}}}""");
    }

    [Fact]
    public void Issue38_FileUploadResponse_ShouldExposeFileIdAndToken()
    {
        var result = FilesApi.ParseUploadResponse("""{"file_id":123456,"token":"file-token"}""");

        result.FileId.Should().Be(123456);
        result.Token.Should().Be("file-token");
    }

    [Fact]
    public void Issue38_XmlUploadResponse_ShouldNotBeTreatedAsToken()
    {
        var result = FilesApi.ParseUploadResponse("<retval>1</retval>");

        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task Issue38_UploadAsync_ShouldUseUploadTokenForVideo()
    {
        _http.Setup(x => x.SendAsyncRaw(It.Is<MaxApiRequest>(r => r.Endpoint == "/uploads"), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"url":"https://vu.oneme.ru/upload?id=1","token":"video-token"}""");
        _http.Setup(x => x.SendAsyncRaw("https://vu.oneme.ru/upload?id=1", It.IsAny<Func<HttpContent?>?>(), It.IsAny<CancellationToken>(), It.IsAny<HttpMethod?>()))
            .ReturnsAsync("<retval>1</retval>");

        var api = new FilesApi(_http.Object, _options);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await api.UploadAsync(UploadType.Video, stream, "clip.mp4");

        result.Token.Should().Be("video-token");
        MaxJsonSerializer.Serialize(AttachmentRequest.FromUpload(UploadType.Video, result))
            .Should().Be("""{"type":"video","payload":{"token":"video-token"}}""");
    }

    [Fact]
    public async Task UploadFileDataAsync_ShouldRewindStreamOnRetry()
    {
        var uploadedLengths = new List<long>();
        _http.Setup(x => x.SendAsyncRaw(It.IsAny<string>(), It.IsAny<Func<HttpContent?>?>(), It.IsAny<CancellationToken>(), It.IsAny<HttpMethod?>()))
            .Returns<string, Func<HttpContent?>?, CancellationToken, HttpMethod?>(async (_, factory, _, _) =>
            {
                // Simulate a failed first attempt followed by a retry.
                for (var i = 0; i < 2; i++)
                {
                    using var content = factory!();
                    var bytes = await content!.ReadAsByteArrayAsync();
                    uploadedLengths.Add(bytes.Length);
                }

                return """{"token":"t"}""";
            });

        var api = new FilesApi(_http.Object, _options);
        using var stream = new MemoryStream(Enumerable.Repeat((byte)7, 1000).ToArray());
        await api.UploadFileDataAsync("https://upload", stream, "file.bin");

        uploadedLengths.Should().HaveCount(2);
        uploadedLengths[1].Should().Be(uploadedLengths[0]);
    }

    #endregion

    #region Networking

    [Fact]
    public void UserAgent_ShouldUsePackageVersion()
    {
        MaxHttpClient.UserAgent.Should().StartWith("MaxMessenger.Bot/");
        MaxHttpClient.UserAgent.Should().NotContain("0.5.1-alpha");
        MaxHttpClient.UserAgent.Should().NotContain("+");
    }

    #endregion

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}
