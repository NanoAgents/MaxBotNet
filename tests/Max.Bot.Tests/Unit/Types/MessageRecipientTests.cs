// 📁 MessageRecipientTests - Tests for MessageRecipient serialization
// 🎯 Core function: Validates ChatId/UserId handling including zero and null values
// 🔗 Key dependencies: FluentAssertions, MaxJsonSerializer, MessageRecipient
// 💡 Usage: Protects fix for direct messages where ChatId can be 0 or null

using FluentAssertions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Max.Bot.Types.Enums;

namespace Max.Bot.Tests.Unit.Types;

public class MessageRecipientTests
{
    [Fact]
    public void Deserialize_ShouldAcceptChatIdZero()
    {
        // Arrange - direct message scenario where Max API sends ChatId = 0
        var json = """{"chat_id":0,"chat_type":"dialog","user_id":12345}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<MessageRecipient>(json);

        // Assert
        result.Should().NotBeNull();
        result!.ChatId.Should().Be(0);
        result.ChatType.Should().Be(ChatType.Dialog);
        result.UserId.Should().Be(12345);
    }

    [Fact]
    public void Deserialize_ShouldAcceptNullChatId()
    {
        // Arrange - direct message scenario where Max API doesn't send ChatId
        var json = """{"chat_type":"dialog","user_id":67890}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<MessageRecipient>(json);

        // Assert
        result.Should().NotBeNull();
        result!.ChatId.Should().BeNull();
        result.ChatType.Should().Be(ChatType.Dialog);
        result.UserId.Should().Be(67890);
    }

    [Fact]
    public void Deserialize_ShouldAcceptPositiveChatId()
    {
        // Arrange - group/channel chat scenario
        var json = """{"chat_id":987654321,"chat_type":"chat"}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<MessageRecipient>(json);

        // Assert
        result.Should().NotBeNull();
        result!.ChatId.Should().Be(987654321);
        result.ChatType.Should().Be(ChatType.Chat);
    }

    [Fact]
    public void Deserialize_ShouldAcceptUserIdZero()
    {
        // Arrange - edge case where UserId might be 0
        var json = """{"chat_id":123456,"chat_type":"chat","user_id":0}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<MessageRecipient>(json);

        // Assert
        result.Should().NotBeNull();
        result!.ChatId.Should().Be(123456);
        result.UserId.Should().Be(0);
    }

    [Fact]
    public void Deserialize_ShouldAcceptNullUserId()
    {
        // Arrange - group chat without specific user
        var json = """{"chat_id":123456,"chat_type":"chat"}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<MessageRecipient>(json);

        // Assert
        result.Should().NotBeNull();
        result!.ChatId.Should().Be(123456);
        result.ChatType.Should().Be(ChatType.Chat);
        result.UserId.Should().BeNull();
    }

    [Fact]
    public void Serialize_ShouldSerializeChatIdZero()
    {
        // Arrange
        var recipient = new MessageRecipient
        {
            ChatId = 0,
            ChatType = ChatType.Dialog,
            UserId = 12345
        };

        // Act
        var json = MaxJsonSerializer.Serialize(recipient);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"chat_id\":0");
        json.Should().Contain("\"chat_type\":\"dialog\"");
        json.Should().Contain("\"user_id\":12345");
    }

    [Fact]
    public void Serialize_ShouldNotIncludeNullChatId()
    {
        // Arrange
        var recipient = new MessageRecipient
        {
            ChatType = ChatType.Dialog,
            UserId = 67890
        };

        // Act
        var json = MaxJsonSerializer.Serialize(recipient);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().NotContain("chat_id");
        json.Should().Contain("\"chat_type\":\"dialog\"");
        json.Should().Contain("\"user_id\":67890");
    }

    [Fact]
    public void Serialize_ShouldSerializePositiveChatId()
    {
        // Arrange
        var recipient = new MessageRecipient
        {
            ChatId = 987654321,
            ChatType = ChatType.Chat
        };

        // Act
        var json = MaxJsonSerializer.Serialize(recipient);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"chat_id\":987654321");
        json.Should().Contain("\"chat_type\":\"chat\"");
    }
}

