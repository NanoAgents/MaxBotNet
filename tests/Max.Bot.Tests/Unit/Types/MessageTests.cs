// 📁 MessageTests.cs - Unit coverage for Message DTO
// 🎯 Core function: Verifies JSON serialization and deserialization of Message fields.
// 🔗 Key dependencies: MaxJsonSerializer, FluentAssertions, Message types.
// 💡 Usage: Guards Max API payload contract for updates and message objects.

using FluentAssertions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Xunit;

namespace Max.Bot.Tests.Unit.Types;

public class MessageTests
{
    [Fact]
    public void Deserialize_ShouldDeserializeMessage()
    {
        // Arrange - API format: sender + recipient + body + timestamp(ms)
        var json = """{"sender":{"user_id":789,"username":"testuser","is_bot":false},"recipient":{"chat_id":456,"chat_type":"dialog"},"timestamp":1609459200000,"body":{"mid":"mid.123","text":"Hello"}}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Message>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Recipient.Should().NotBeNull();
        result.Recipient!.ChatId.Should().Be(456);
        result.Recipient.ChatType.Should().Be(ChatType.Dialog);
        result.Sender.Should().NotBeNull();
        result.Sender!.Id.Should().Be(789);
        result.Sender.Username.Should().Be("testuser");
        result.Text.Should().Be("Hello");
        result.Timestamp.Should().Be(1609459200000);
        result.Body.Should().NotBeNull();
        result.Body!.Mid.Should().Be("mid.123");
    }

    [Fact]
    public void Deserialize_ShouldDeserializeMessageWithNullFields()
    {
        // Arrange
        var json = """{}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Message>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Recipient.Should().NotBeNull();
        result.Recipient.ChatType.Should().BeNull();
        result.Text.Should().BeNull();
        result.Timestamp.Should().Be(0);
    }

    [Fact]
    public void Serialize_ShouldSerializeMessage()
    {
        // Arrange
        var message = new Message
        {
            Sender = new User { Id = 789, Username = "testuser" },
            Recipient = new MessageRecipient { ChatId = 456, ChatType = ChatType.Dialog },
            Body = new MessageBody { Mid = "mid.123", Text = "Hello" },
            Timestamp = 1609459200000
        };

        // Act
        var json = MaxJsonSerializer.Serialize(message);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"sender\"");
        json.Should().Contain("\"recipient\"");
        json.Should().Contain("\"body\"");
        json.Should().Contain("\"timestamp\":1609459200000");
        json.Should().Contain("\"text\":\"Hello\"");
    }

    [Fact]
    public void Serialize_ShouldNotIncludeNullFields()
    {
        // Arrange
        var message = new Message
        {
            Timestamp = 1609459200000
        };

        // Act
        var json = MaxJsonSerializer.Serialize(message);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"timestamp\":1609459200000");
        json.Should().NotContain("\"sender\"");
        json.Should().Contain("\"recipient\":{}");
        json.Should().NotContain("\"text\"");
    }

    [Fact]
    public void Deserialize_ShouldDeserializeMessageWithNewFields()
    {
        // Arrange
        var json = """{"sender":{"user_id":789,"username":"testuser"},"recipient":{"chat_id":456,"chat_type":"dialog"},"timestamp":1609459200000,"body":{"mid":"mid.123","text":"Hello","attachments":[]},"stat":{"read_count":5},"url":"https://max.ru/message/123"}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Message>(json);

        // Assert
        result.Should().NotBeNull();
        result.Sender.Should().NotBeNull();
        result.Sender!.Id.Should().Be(789);
        result.Timestamp.Should().Be(1609459200000);
        result.Body.Should().NotBeNull();
        result.Body!.Text.Should().Be("Hello");
        result.Stat.Should().NotBeNull();
        result.Stat!.ReadCount.Should().Be(5);
        result.Url.Should().Be("https://max.ru/message/123");
    }

    [Fact]
    public void Deserialize_ShouldDeserializeMessageWithLink()
    {
        // Arrange - using official API structure: link.message contains MessageBody directly (mid, seq, text)
        var json = """{"timestamp":1609459200000,"link":{"type":"reply","chat_id":456,"sender":{"user_id":789},"message":{"mid":"mid.orig.1","seq":123456,"text":"Original message"}}}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Message>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Link.Should().NotBeNull();
        result.Link!.Type.Should().Be(MessageLinkType.Reply);
        result.Link.ChatId.Should().Be(456);
        result.Link.Sender.Should().NotBeNull();
        result.Link.Sender!.Id.Should().Be(789);
        result.Link.Mid.Should().Be("mid.orig.1");
        result.Link.Seq.Should().Be(123456);
        result.Link.Text.Should().Be("Original message");
    }

    [Fact]
    public void Serialize_ShouldSerializeMessageWithNewFields()
    {
        // Arrange
        var message = new Message
        {
            Sender = new User { Id = 789, Username = "testuser" },
            Recipient = new MessageRecipient { ChatId = 456, ChatType = ChatType.Dialog },
            Timestamp = 1609459200000,
            Body = new MessageBody { Mid = "mid.123", Text = "Hello", Attachments = Array.Empty<Attachment>() },
            Stat = new MessageStat { ReadCount = 5 },
            Url = "https://max.ru/message/123"
        };

        // Act
        var json = MaxJsonSerializer.Serialize(message);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"sender\"");
        json.Should().Contain("\"recipient\"");
        json.Should().Contain("\"timestamp\":1609459200000");
        json.Should().Contain("\"body\"");
        json.Should().Contain("\"stat\"");
        json.Should().Contain("\"url\":\"https://max.ru/message/123\"");
    }

    [Fact]
    public void MessageBody_ShouldDeserialize()
    {
        // Arrange — flat format matching real API response
        var json = """{"text":"Hello","attachments":[{"type":"image","id":1,"file_id":"photo1","width":100,"height":100}]}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<MessageBody>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Text.Should().Be("Hello");
        result.Attachments.Should().NotBeNull();
        result.Attachments!.Length.Should().Be(1);
        result.Attachments[0].Should().BeOfType<PhotoAttachment>();
        var photo = (PhotoAttachment)result.Attachments[0];
        photo.Id.Should().Be(1);
        photo.FileId.Should().Be("photo1");
        photo.Width.Should().Be(100);
        photo.Height.Should().Be(100);
    }

    [Fact]
    public void MessageStat_ShouldDeserialize()
    {
        // Arrange
        var json = """{"read_count":10}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<MessageStat>(json);

        // Assert
        result.Should().NotBeNull();
        result!.ReadCount.Should().Be(10);
    }
}

