using FluentAssertions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Xunit;

namespace Max.Bot.Tests.Unit.Types;

public class CallbackQueryTests
{
    [Fact]
    public void CallbackQuery_ShouldDeserialize_FromJson()
    {
        // Arrange - API format
        var json = """{"callback_id":"cb123","user":{"user_id":123,"username":"user123","is_bot":false},"message":{"body":{"mid":"msg456","text":"Test message"},"timestamp":1234567890000},"payload":"payload123","timestamp":1609459200000}""";

        // Act
        var callbackQuery = MaxJsonSerializer.Deserialize<CallbackQuery>(json);

        // Assert
        callbackQuery.Should().NotBeNull();
        callbackQuery.CallbackId.Should().Be("cb123");
        callbackQuery.User.Should().NotBeNull();
        callbackQuery.User!.Id.Should().Be(123);
        callbackQuery.User.Username.Should().Be("user123");
        callbackQuery.Message.Should().BeNull();
        callbackQuery.Payload.Should().Be("payload123");
        callbackQuery.Timestamp.Should().Be(1609459200000);
    }

    [Fact]
    public void CallbackQuery_ShouldDeserialize_WithNullableFields()
    {
        // Arrange - API format with optional fields
        var json = """{"callback_id":"cb123","user":{"user_id":123,"username":"user123","is_bot":false}}""";

        // Act
        var callbackQuery = MaxJsonSerializer.Deserialize<CallbackQuery>(json);

        // Assert
        callbackQuery.Should().NotBeNull();
        callbackQuery.CallbackId.Should().Be("cb123");
        callbackQuery.User.Should().NotBeNull();
        callbackQuery.User!.Id.Should().Be(123);
        callbackQuery.Message.Should().BeNull();
        callbackQuery.Payload.Should().BeNull();
        callbackQuery.Timestamp.Should().BeNull();
    }

    [Fact]
    public void CallbackQuery_ShouldSerialize_ToJson()
    {
        // Arrange - API format
        var callbackQuery = new CallbackQuery
        {
            CallbackId = "cb123",
            User = new User
            {
                Id = 123,
                Username = "user123",
                IsBot = false
            },
            Payload = "payload123",
            Timestamp = 1609459200000
        };

        // Act
        var json = MaxJsonSerializer.Serialize(callbackQuery);

        // Assert
        json.Should().Contain("\"callback_id\":\"cb123\"");
        json.Should().Contain("\"user\"");
        json.Should().Contain("\"payload\":\"payload123\"");
        json.Should().Contain("\"timestamp\":1609459200000");
    }
}

