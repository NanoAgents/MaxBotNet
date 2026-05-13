// 📁 MessagesApiTests.cs - Unit tests for MessagesApi endpoints
// 🎯 Core function: Verifies request building and response mapping for messages.
// 🔗 Key dependencies: Moq IMaxHttpClient, MaxJsonSerializer, MessagesApi.
// 💡 Usage: Ensures strict Max API message structure (recipient/body/timestamp).

using System.Net;
using System.Net.Http;
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

namespace Max.Bot.Tests.Unit.Api;

public class MessagesApiTests
{
    private readonly Mock<IMaxHttpClient> _mockHttpClient;
    private readonly MaxBotOptions _options;

    public MessagesApiTests()
    {
        _mockHttpClient = new Mock<IMaxHttpClient>();
        _options = new MaxBotOptions
        {
            Token = "test-token-123",
            BaseUrl = "https://api.max.ru/bot"
        };
    }

    [Fact]
    public async Task SendMessageAsync_ShouldReturnMessage_WhenRequestSucceeds()
    {
        // Arrange
        var chatId = 123456L;
        var text = "Hello, World!";
        var expectedMessage = new Message
        {
            Timestamp = 1609459200000,
            Recipient = new MessageRecipient { ChatId = chatId, ChatType = "chat" },
            Body = new MessageBody { Mid = "mid.789", Text = text }
        };

        var response = new MessageResponse
        {
            Message = expectedMessage
        };

        _mockHttpClient
            .Setup(x => x.SendAsync<MessageResponse>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Post &&
                    req.Endpoint == "/messages" &&
                    req.Body != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.SendMessageAsync(chatId, text);

        // Assert
        result.Should().NotBeNull();
        result.Mid.Should().Be("mid.789");
        result.Text.Should().Be(text);
        result.Recipient!.ChatId.Should().Be(chatId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendMessageAsync_ShouldThrowArgumentException_WhenTextIsNullOrEmpty(string? text)
    {
        // Arrange
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.SendMessageAsync(123456L, text!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("text");
    }

    [Fact]
    public async Task GetMessagesAsync_ShouldReturnMessages_WhenRequestSucceeds()
    {
        // Arrange
        var chatId = 123456L;
        var expectedMessages = new[]
        {
            new Message { Timestamp = 1609459200000, Recipient = new MessageRecipient { ChatId = chatId, ChatType = "chat" }, Body = new MessageBody { Mid = "mid.1", Text = "Message 1" } },
            new Message { Timestamp = 1609459200000, Recipient = new MessageRecipient { ChatId = chatId, ChatType = "chat" }, Body = new MessageBody { Mid = "mid.2", Text = "Message 2" } }
        };

        var response = new GetMessagesResponse
        {
            Messages = expectedMessages
        };

        var responseJson = MaxJsonSerializer.Serialize(response);
        _mockHttpClient
            .Setup(x => x.SendAsyncRaw(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Endpoint == "/messages" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("chat_id") &&
                    req.QueryParameters["chat_id"] == chatId.ToString()),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.GetMessagesAsync(chatId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Mid.Should().Be("mid.1");
        result[1].Mid.Should().Be("mid.2");
    }

    [Fact]
    public async Task EditMessageAsync_ShouldReturnResponse_WhenRequestSucceeds()
    {
        // Arrange
        var messageId = "msg-123";
        var request = new EditMessageRequest
        {
            Text = "Updated text",
            Format = TextFormat.Markdown
        };

        var response = new Response
        {
            Success = true,
            Message = "Message edited successfully"
        };

        _mockHttpClient
            .Setup(x => x.SendAsync<Response>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Put &&
                    req.Endpoint == "/messages" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("message_id") &&
                    req.QueryParameters["message_id"] == messageId &&
                    req.Body != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.EditMessageAsync(messageId, request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Message edited successfully");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EditMessageAsync_ShouldThrowArgumentException_WhenMessageIdIsNullOrEmpty(string? messageId)
    {
        // Arrange
        var request = new EditMessageRequest { Text = "Updated text" };
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.EditMessageAsync(messageId!, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    [Fact]
    public async Task EditMessageAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.EditMessageAsync("msg-123", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task EditMessageReplyMarkupAsync_ShouldRemoveKeyboard_WhenKeyboardIsNull()
    {
        // Arrange
        var messageId = "msg-123";
        var currentMessage = new Message
        {
            Text = "Test message",
            Recipient = new MessageRecipient { ChatId = 456, ChatType = "chat" },
            Body = new MessageBody
            {
                Mid = "mid.current.123",
                Attachments = Array.Empty<Attachment>()
            }
        };

        var getMessageResponse = new Response<Message>
        {
            Ok = true,
            Result = currentMessage
        };

        var getMessageResponseJson = MaxJsonSerializer.Serialize(getMessageResponse);
        _mockHttpClient
            .Setup(x => x.SendAsyncRaw(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Endpoint == $"/messages/{messageId}"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(getMessageResponseJson);

        var editResponse = new Response
        {
            Success = true,
            Message = "Keyboard removed successfully"
        };

        _mockHttpClient
            .Setup(x => x.SendAsync<Response>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Put &&
                    req.Endpoint == "/messages" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("message_id") &&
                    req.QueryParameters["message_id"] == messageId &&
                    req.Body != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(editResponse);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.EditMessageReplyMarkupAsync(messageId, null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Keyboard removed successfully");
    }

    [Fact]
    public async Task EditMessageReplyMarkupAsync_ShouldReplaceKeyboard_WhenKeyboardIsProvided()
    {
        // Arrange
        var messageId = "msg-123";
        var keyboard = new InlineKeyboard(new[]
        {
            new[]
            {
                new InlineKeyboardButton { Text = "Button 1", Payload = "btn1", Type = ButtonType.Callback }
            }
        });
        var currentMessage = new Message
        {
            Text = "Test message",
            Recipient = new MessageRecipient { ChatId = 456, ChatType = "chat" },
            Body = new MessageBody
            {
                Mid = "mid.current.123",
                Attachments = Array.Empty<Attachment>()
            }
        };

        var getMessageResponse = new Response<Message>
        {
            Ok = true,
            Result = currentMessage
        };

        var getMessageResponseJson = MaxJsonSerializer.Serialize(getMessageResponse);
        _mockHttpClient
            .Setup(x => x.SendAsyncRaw(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Endpoint == $"/messages/{messageId}"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(getMessageResponseJson);

        var editResponse = new Response
        {
            Success = true,
            Message = "Keyboard replaced successfully"
        };

        _mockHttpClient
            .Setup(x => x.SendAsync<Response>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Put &&
                    req.Endpoint == "/messages" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("message_id") &&
                    req.QueryParameters["message_id"] == messageId &&
                    req.Body != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(editResponse);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.EditMessageReplyMarkupAsync(messageId, keyboard);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Keyboard replaced successfully");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EditMessageReplyMarkupAsync_ShouldThrowArgumentException_WhenMessageIdIsNullOrEmpty(string? messageId)
    {
        // Arrange
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.EditMessageReplyMarkupAsync(messageId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    [Fact]
    public async Task DeleteMessageAsync_ShouldReturnResponse_WhenRequestSucceeds()
    {
        // Arrange
        var messageId = "msg-123";
        var response = new Response
        {
            Success = true,
            Message = "Message deleted successfully"
        };

        _mockHttpClient
            .Setup(x => x.SendAsync<Response>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.Endpoint == "/messages" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("message_id") &&
                    req.QueryParameters["message_id"] == messageId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.DeleteMessageAsync(messageId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Message deleted successfully");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteMessageAsync_ShouldThrowArgumentException_WhenMessageIdIsNullOrEmpty(string? messageId)
    {
        // Arrange
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.DeleteMessageAsync(messageId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    [Fact]
    public async Task GetMessageAsync_ShouldReturnMessage_WhenRequestSucceeds()
    {
        // Arrange
        var messageId = "msg-123";
        var expectedMessage = new Message
        {
            Text = "Test message",
            Timestamp = 1609459200000,
            Recipient = new MessageRecipient { ChatId = 456, ChatType = "chat" },
            Body = new MessageBody { Mid = "msg-123", Text = "Test message" }
        };

        var response = new Response<Message>
        {
            Ok = true,
            Result = expectedMessage
        };

        var responseJson = MaxJsonSerializer.Serialize(response);
        _mockHttpClient
            .Setup(x => x.SendAsyncRaw(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Endpoint == $"/messages/{messageId}"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.GetMessageAsync(messageId);

        // Assert
        result.Should().NotBeNull();
        result.Mid.Should().Be("msg-123");
        result.Text.Should().Be("Test message");
        result.Recipient!.ChatId.Should().Be(456);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetMessageAsync_ShouldThrowArgumentException_WhenMessageIdIsNullOrEmpty(string? messageId)
    {
        // Arrange
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.GetMessageAsync(messageId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    [Fact]
    public async Task GetVideoAsync_ShouldReturnVideo_WhenRequestSucceeds()
    {
        // Arrange
        var videoToken = "video-token-123";
        var expectedVideo = new Video
        {
            Id = 789,
            FileId = "file-123",
            Width = 1920,
            Height = 1080,
            Duration = 60
        };

        var response = new Response<Video>
        {
            Ok = true,
            Result = expectedVideo
        };

        var responseJson = MaxJsonSerializer.Serialize(response);
        _mockHttpClient
            .Setup(x => x.SendAsyncRaw(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Endpoint == $"/videos/{videoToken}"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.GetVideoAsync(videoToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedVideo.Id);
        result.FileId.Should().Be(expectedVideo.FileId);
        result.Width.Should().Be(expectedVideo.Width);
        result.Height.Should().Be(expectedVideo.Height);
        result.Duration.Should().Be(expectedVideo.Duration);
    }

    [Fact]
    public async Task GetVideoAsync_ShouldMapTokenUrlsAndThumbnail_WhenResponseUsesNewShape()
    {
        // Arrange
        var videoToken = "video-token-123";
        var responseJson = """
            {
              "ok": true,
              "result": {
                "token": "f9LHodD0",
                "width": 720,
                "height": 1280,
                "duration": 6633,
                "urls": {
                  "mp4_720": "http://vd555.okcdn.ru/video.mp4"
                },
                "thumbnail": {
                  "url": "https://pimg.mycdn.me/thumb.jpg"
                }
              }
            }
            """;

        _mockHttpClient
            .Setup(x => x.SendAsyncRaw(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Endpoint == $"/videos/{videoToken}"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.GetVideoAsync(videoToken);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("f9LHodD0");
        result.Width.Should().Be(720);
        result.Height.Should().Be(1280);
        result.Duration.Should().Be(6633);
        result.Urls.Should().NotBeNull();
        result.Urls!["mp4_720"].Should().Be("http://vd555.okcdn.ru/video.mp4");
        result.Thumbnail.Should().NotBeNull();
        result.Thumbnail!.Url.Should().Be("https://pimg.mycdn.me/thumb.jpg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetVideoAsync_ShouldThrowArgumentException_WhenVideoTokenIsNullOrEmpty(string? videoToken)
    {
        // Arrange
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.GetVideoAsync(videoToken!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("videoToken");
    }

    [Fact]
    public async Task AnswerCallbackQueryAsync_ShouldReturnResponse_WhenRequestSucceeds()
    {
        // Arrange
        var callbackQueryId = "callback-123";
        var request = new AnswerCallbackQueryRequest
        {
            Notification = "Button clicked!",
            Message = new NewMessageBody
            {
                Text = "Updated message text"
            }
        };

        var response = new Response
        {
            Success = true,
            Message = "Callback query answered successfully"
        };

        _mockHttpClient
            .Setup(x => x.SendAsync<Response>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Post &&
                    req.Endpoint == "/answers" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("callback_query_id") &&
                    req.QueryParameters["callback_query_id"] == callbackQueryId &&
                    req.Body != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.AnswerCallbackQueryAsync(callbackQueryId, request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Callback query answered successfully");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnswerCallbackQueryAsync_ShouldThrowArgumentException_WhenCallbackQueryIdIsNullOrEmpty(string? callbackQueryId)
    {
        // Arrange
        var request = new AnswerCallbackQueryRequest { Notification = "Test" };
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.AnswerCallbackQueryAsync(callbackQueryId!, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("callbackQueryId");
    }

    [Fact]
    public async Task AnswerCallbackQueryAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.AnswerCallbackQueryAsync("callback-123", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task SendMessageAsync_WithSendMessageRequest_ShouldReturnMessage_WhenRequestSucceeds()
    {
        // Arrange
        var chatId = 123456L;
        var request = new SendMessageRequest
        {
            Text = "Hello, World!",
            Notify = true,
            Format = TextFormat.Markdown
        };

        var expectedMessage = new Message
        {
            Text = request.Text,
            Timestamp = 1609459200000,
            Recipient = new MessageRecipient { ChatId = chatId, ChatType = "chat" },
            Body = new MessageBody { Mid = "mid.789", Text = request.Text }
        };

        var response = new MessageResponse
        {
            Message = expectedMessage
        };

        _mockHttpClient
            .Setup(x => x.SendAsync<MessageResponse>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Post &&
                    req.Endpoint == "/messages" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("chat_id") &&
                    req.QueryParameters["chat_id"] == chatId.ToString() &&
                    req.Body != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.SendMessageAsync(request, chatId: chatId);

        // Assert
        result.Should().NotBeNull();
        result.Mid.Should().Be("mid.789");
        result.Text.Should().Be(request.Text);
    }

    [Fact]
    public async Task SendMessageAsync_WithSendMessageRequest_ShouldThrowArgumentException_WhenBothChatIdAndUserIdProvided()
    {
        // Arrange
        var request = new SendMessageRequest { Text = "Test" };
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.SendMessageAsync(request, chatId: 1, userId: 2);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*chatId*userId*");
    }

    [Fact]
    public async Task SendMessageAsync_WithSendMessageRequest_ShouldThrowArgumentException_WhenNeitherChatIdNorUserIdProvided()
    {
        // Arrange
        var request = new SendMessageRequest { Text = "Test" };
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.SendMessageAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*chatId*userId*");
    }

    [Fact]
    public async Task SendMessageAsync_WithSendMessageRequest_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.SendMessageAsync(null!, chatId: 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task SendMessageWithAttachmentAsync_ShouldReturnMessage_WhenRequestSucceeds()
    {
        // Arrange
        var chatId = 123456L;
        var attachment = new AttachmentRequest
        {
            Type = "image",
            Payload = new { token = "test-token" }
        };

        var expectedMessage = new Message
        {
            Text = "With attachment",
            Timestamp = 1609459200000,
            Recipient = new MessageRecipient { ChatId = chatId, ChatType = "chat" },
            Body = new MessageBody { Mid = "mid.789", Text = "With attachment" }
        };

        var response = new MessageResponse
        {
            Message = expectedMessage
        };

        _mockHttpClient
            .Setup(x => x.SendAsync<MessageResponse>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Post &&
                    req.Endpoint == "/messages" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("chat_id") &&
                    req.Body != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.SendMessageWithAttachmentAsync(attachment, chatId: chatId, text: "With attachment");

        // Assert
        result.Should().NotBeNull();
        result.Mid.Should().Be("mid.789");
    }

    [Fact]
    public async Task SendMessageWithAttachmentAsync_ShouldThrowArgumentNullException_WhenAttachmentIsNull()
    {
        // Arrange
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.SendMessageWithAttachmentAsync(null!, chatId: 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("attachment");
    }

    [Fact]
    public async Task SendMessageWithAttachmentAsync_ShouldRetry_WhenApiReturnsBadRequestOnce()
    {
        // Arrange
        var chatId = 123456L;
        var attachment = new AttachmentRequest
        {
            Type = "file",
            Payload = new { token = "test-token" }
        };

        var expectedMessage = new Message
        {
            Text = "With attachment",
            Timestamp = 1609459200000,
            Recipient = new MessageRecipient { ChatId = chatId, ChatType = "chat" },
            Body = new MessageBody { Mid = "mid.retry", Text = "With attachment" }
        };

        _mockHttpClient
            .SetupSequence(x => x.SendAsync<MessageResponse>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Post &&
                    req.Endpoint == "/messages" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("chat_id") &&
                    req.Body != null),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MaxApiException("attachment is not ready", null, HttpStatusCode.BadRequest))
            .ReturnsAsync(new MessageResponse { Message = expectedMessage });

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.SendMessageWithAttachmentAsync(attachment, chatId: chatId, text: "With attachment");

        // Assert
        result.Should().NotBeNull();
        result.Mid.Should().Be("mid.retry");
        _mockHttpClient.Verify(x => x.SendAsync<MessageResponse>(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ForwardMessageAsync_ShouldReturnMessage_WhenRequestSucceeds()
    {
        // Arrange
        var chatId = 123456L;
        var messageId = "789";

        var expectedMessage = new Message
        {
            Text = "Forwarded",
            Timestamp = 1609459200000,
            Recipient = new MessageRecipient { ChatId = chatId, ChatType = "chat" },
            Body = new MessageBody { Mid = "mid.999", Text = "Forwarded" }
        };

        var response = new MessageResponse
        {
            Message = expectedMessage
        };

        _mockHttpClient
            .Setup(x => x.SendAsync<MessageResponse>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Post &&
                    req.Endpoint == "/messages" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("chat_id") &&
                    req.Body != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.ForwardMessageAsync(messageId, chatId: chatId);

        // Assert
        result.Should().NotBeNull();
        result.Mid.Should().Be("mid.999");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ForwardMessageAsync_ShouldThrowArgumentException_WhenMessageIdIsNullOrEmpty(string? messageId)
    {
        // Arrange
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.ForwardMessageAsync(messageId!, chatId: 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    [Fact]
    public async Task ReplyToMessageAsync_ShouldReturnMessage_WhenRequestSucceeds()
    {
        // Arrange
        var chatId = 123456L;
        var messageId = "789";
        var text = "Reply text";

        var expectedMessage = new Message
        {
            Text = text,
            Timestamp = 1609459200000,
            Recipient = new MessageRecipient { ChatId = chatId, ChatType = "chat" },
            Body = new MessageBody { Mid = "mid.999", Text = text }
        };

        var response = new MessageResponse
        {
            Message = expectedMessage
        };

        _mockHttpClient
            .Setup(x => x.SendAsync<MessageResponse>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Post &&
                    req.Endpoint == "/messages" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("chat_id") &&
                    req.Body != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var result = await messagesApi.ReplyToMessageAsync(messageId, text, chatId: chatId);

        // Assert
        result.Should().NotBeNull();
        result.Mid.Should().Be("mid.999");
        result.Text.Should().Be(text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReplyToMessageAsync_ShouldThrowArgumentException_WhenMessageIdIsNullOrEmpty(string? messageId)
    {
        // Arrange
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.ReplyToMessageAsync(messageId!, "text", chatId: 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("messageId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReplyToMessageAsync_ShouldThrowArgumentException_WhenTextIsNullOrEmpty(string? text)
    {
        // Arrange
        var messagesApi = new MessagesApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await messagesApi.ReplyToMessageAsync("1", text!, chatId: 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("text");
    }
}

