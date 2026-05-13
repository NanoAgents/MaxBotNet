// 📁 UpdateTests.cs - Unit coverage for Update JSON contract
// 🎯 Core function: Validates UpdateJsonConverter and typed wrapper properties.
// 🔗 Key dependencies: MaxJsonSerializer, UpdateJsonConverter, UpdateType enum.
// 💡 Usage: Prevents regressions in update parsing across supported update types.

using FluentAssertions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Xunit;

namespace Max.Bot.Tests.Unit.Types;

public class UpdateTests
{
    #region Message Created Tests

    [Fact]
    public void Deserialize_MessageCreated_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":1,"update_type":"message_created","timestamp":1609459200000,"user_locale":"ru","message":{"sender":{"user_id":789,"username":"testuser","is_bot":false},"recipient":{"chat_id":456,"chat_type":"dialog"},"body":{"mid":"mid.123","text":"Hello"},"timestamp":1609459200000}}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(1);
        result.Type.Should().Be(UpdateType.MessageCreated);
        result.UpdateTypeRaw.Should().Be("message_created");
        result.Timestamp.Should().Be(1609459200000);
        result.UserLocale.Should().Be("ru");
        result.Message.Should().NotBeNull();
        result.Message!.Sender.Should().NotBeNull();
        result.Message.Sender!.Id.Should().Be(789);
        result.Message.Body.Should().NotBeNull();
        result.Message.Body!.Text.Should().Be("Hello");
        result.Message.Text.Should().Be("Hello"); // Convenience property

        // Test typed wrapper
        result.MessageUpdate.Should().NotBeNull();
        result.MessageUpdate!.UpdateId.Should().Be(1);
        result.MessageUpdate.Message.Should().NotBeNull();
        result.MessageUpdate.Message.Text.Should().Be("Hello");
    }

    [Fact]
    public void Deserialize_WebhookUpdate_ShouldDeserializeCorrectly()
    {
        // Arrange - webhook format with body.mid
        var json = """{"message":{"recipient":{"chat_id":79313411,"chat_type":"dialog","user_id":94399782},"timestamp":1763928007254,"body":{"mid":"mid.0000000004ba3a03019ab24d62566a52","seq":115600785883425360,"text":"/start"},"sender":{"user_id":18503461,"first_name":"Александр","last_name":"Сюзев","is_bot":false,"last_activity_time":1763927992000,"name":"Александр Сюзев"}},"timestamp":1763928007254,"user_locale":"ru","update_type":"message_created"}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateTypeRaw.Should().Be("message_created");
        result.Type.Should().Be(UpdateType.MessageCreated);
        result.Message.Should().NotBeNull();
        result.Message!.Body.Should().NotBeNull();
        result.Message.Body!.Mid.Should().Be("mid.0000000004ba3a03019ab24d62566a52");
        result.Message.Body.Text.Should().Be("/start");
        result.Message.Mid.Should().Be("mid.0000000004ba3a03019ab24d62566a52"); // Convenience property
        result.Message.Sender.Should().NotBeNull();
        result.Message.Sender!.Id.Should().Be(18503461);
        result.Message.Recipient.Should().NotBeNull();
        result.Message.Recipient!.ChatId.Should().Be(79313411);
    }

    #endregion

    #region Message Callback Tests

    [Fact]
    public void Deserialize_MessageCallback_ShouldDeserializeCorrectly()
    {
        // Arrange - API format uses "callback" field
        var json = """{"update_id":2,"update_type":"message_callback","timestamp":1609459200000,"message":{"body":{"mid":"mid.cb.1","text":"Button host"},"timestamp":1609459200000},"callback":{"callback_id":"cb123","user":{"user_id":123,"username":"user123","is_bot":false},"payload":"button_clicked","timestamp":1609459200000}}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(2);
        result.Type.Should().Be(UpdateType.MessageCallback);
        result.UpdateTypeRaw.Should().Be("message_callback");
        result.Callback.Should().NotBeNull();
        result.Callback!.CallbackId.Should().Be("cb123");
        result.Callback.User.Should().NotBeNull();
        result.Callback.User!.Id.Should().Be(123);
        result.Callback.Payload.Should().Be("button_clicked");
        result.Callback.Timestamp.Should().Be(1609459200000);

        // Test typed wrapper
        result.CallbackQueryUpdate.Should().NotBeNull();
        result.CallbackQueryUpdate!.UpdateId.Should().Be(2);
        result.CallbackQueryUpdate.CallbackQuery.Should().NotBeNull();
        result.CallbackQueryUpdate.CallbackQuery.CallbackId.Should().Be("cb123");
        result.CallbackQueryUpdate.Message.Should().NotBeNull();
        result.CallbackQueryUpdate.Message!.Body!.Mid.Should().Be("mid.cb.1");
    }

    #endregion

    #region Message Edited Tests

    [Fact]
    public void Deserialize_MessageEdited_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":3,"update_type":"message_edited","timestamp":1609459200000,"message":{"body":{"mid":"mid.123","text":"Edited text"},"timestamp":1609459200000}}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(3);
        result.Type.Should().Be(UpdateType.MessageEdited);
        result.Message.Should().NotBeNull();
        result.Message!.Body!.Text.Should().Be("Edited text");

        // Test typed wrapper
        result.MessageEditedUpdate.Should().NotBeNull();
        result.MessageEditedUpdate!.Message.Should().NotBeNull();
        result.MessageEditedUpdate.Message.Text.Should().Be("Edited text");
    }

    #endregion

    #region Message Removed Tests

    [Fact]
    public void Deserialize_MessageRemoved_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":4,"update_type":"message_removed","timestamp":1609459200000,"message":{"body":{"mid":"mid.123"}}}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(4);
        result.Type.Should().Be(UpdateType.MessageRemoved);

        // Test typed wrapper
        result.MessageRemovedUpdate.Should().NotBeNull();
        result.MessageRemovedUpdate!.Message.Should().NotBeNull();
    }

    #endregion

    #region Bot Added Tests

    [Fact]
    public void Deserialize_BotAdded_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":5,"update_type":"bot_added","timestamp":1609459200000,"chat":{"chat_id":123,"type":"chat","title":"Test Chat"},"user":{"user_id":456,"username":"admin","is_bot":false},"inviter_id":789}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(5);
        result.Type.Should().Be(UpdateType.BotAdded);
        result.Chat.Should().NotBeNull();
        result.Chat!.ChatId.Should().Be(123);
        result.Chat.Title.Should().Be("Test Chat");
        result.User.Should().NotBeNull();
        result.User!.Id.Should().Be(456);
        result.InviterId.Should().Be(789);

        // Test typed wrapper
        result.BotAddedUpdate.Should().NotBeNull();
        result.BotAddedUpdate!.Chat.Should().NotBeNull();
        result.BotAddedUpdate.User.Should().NotBeNull();
        result.BotAddedUpdate.InviterId.Should().Be(789);
    }

    #endregion

    #region Bot Removed Tests

    [Fact]
    public void Deserialize_BotRemoved_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":6,"update_type":"bot_removed","timestamp":1609459200000,"chat":{"chat_id":123,"type":"chat"},"user":{"user_id":456,"is_bot":false}}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(6);
        result.Type.Should().Be(UpdateType.BotRemoved);
        result.Chat.Should().NotBeNull();
        result.User.Should().NotBeNull();

        // Test typed wrapper
        result.BotRemovedUpdate.Should().NotBeNull();
    }

    #endregion

    #region Bot Started Tests

    [Fact]
    public void Deserialize_BotStarted_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":7,"update_type":"bot_started","timestamp":1609459200000,"chat_id":123,"user":{"user_id":456,"username":"testuser","is_bot":false},"user_locale":"ru","payload":"promo_summer2025"}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(7);
        result.Type.Should().Be(UpdateType.BotStarted);
        result.ChatId.Should().Be(123);
        result.User.Should().NotBeNull();
        result.User!.Id.Should().Be(456);
        result.UserLocale.Should().Be("ru");
        result.Payload.Should().Be("promo_summer2025");

        // Test typed wrapper
        result.BotStartedUpdate.Should().NotBeNull();
        result.BotStartedUpdate!.ChatId.Should().Be(123);
        result.BotStartedUpdate.User!.Id.Should().Be(456);
        result.BotStartedUpdate.UserLocale.Should().Be("ru");
        result.BotStartedUpdate.Payload.Should().Be("promo_summer2025");
    }

    #endregion

    #region Bot Stopped Tests

    [Fact]
    public void Deserialize_BotStopped_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":8,"update_type":"bot_stopped","timestamp":1609459200000,"chat_id":123,"user":{"user_id":456,"is_bot":false}}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(8);
        result.Type.Should().Be(UpdateType.BotStopped);
        result.ChatId.Should().Be(123);

        // Test typed wrapper
        result.BotStoppedUpdate.Should().NotBeNull();
        result.BotStoppedUpdate!.ChatId.Should().Be(123);
    }

    #endregion

    #region User Added Tests

    [Fact]
    public void Deserialize_UserAdded_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":9,"update_type":"user_added","timestamp":1609459200000,"chat":{"chat_id":123,"type":"chat"},"user":{"user_id":456,"is_bot":false},"inviter_id":789}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(9);
        result.Type.Should().Be(UpdateType.UserAdded);
        result.InviterId.Should().Be(789);

        // Test typed wrapper
        result.UserAddedUpdate.Should().NotBeNull();
        result.UserAddedUpdate!.InviterId.Should().Be(789);
    }

    #endregion

    #region User Removed Tests

    [Fact]
    public void Deserialize_UserRemoved_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":10,"update_type":"user_removed","timestamp":1609459200000,"chat":{"chat_id":123,"type":"chat"},"user":{"user_id":456,"is_bot":false}}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(10);
        result.Type.Should().Be(UpdateType.UserRemoved);

        // Test typed wrapper
        result.UserRemovedUpdate.Should().NotBeNull();
    }

    #endregion

    #region Chat Title Changed Tests

    [Fact]
    public void Deserialize_ChatTitleChanged_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":11,"update_type":"chat_title_changed","timestamp":1609459200000,"chat":{"chat_id":123,"type":"chat","title":"New Title"},"user":{"user_id":456,"is_bot":false}}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(11);
        result.Type.Should().Be(UpdateType.ChatTitleChanged);
        result.Chat.Should().NotBeNull();
        result.Chat!.Title.Should().Be("New Title");

        // Test typed wrapper
        result.ChatTitleChangedUpdate.Should().NotBeNull();
        result.ChatTitleChangedUpdate!.Chat!.Title.Should().Be("New Title");
    }

    #endregion

    #region Dialog Muted Tests

    [Fact]
    public void Deserialize_DialogMuted_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":12,"update_type":"dialog_muted","timestamp":1609459200000,"chat_id":123,"is_muted":true}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(12);
        result.Type.Should().Be(UpdateType.DialogMuted);
        result.ChatId.Should().Be(123);
        result.IsMuted.Should().BeTrue();

        // Test typed wrapper
        result.DialogMutedUpdate.Should().NotBeNull();
        result.DialogMutedUpdate!.IsMuted.Should().BeTrue();
    }

    #endregion

    #region Dialog Unmuted Tests

    [Fact]
    public void Deserialize_DialogUnmuted_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":13,"update_type":"dialog_unmuted","timestamp":1609459200000,"chat_id":123,"is_muted":false}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(13);
        result.Type.Should().Be(UpdateType.DialogUnmuted);
        result.IsMuted.Should().BeFalse();

        // Test typed wrapper
        result.DialogUnmutedUpdate.Should().NotBeNull();
    }

    #endregion

    #region Dialog Cleared Tests

    [Fact]
    public void Deserialize_DialogCleared_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":14,"update_type":"dialog_cleared","timestamp":1609459200000,"chat_id":123}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(14);
        result.Type.Should().Be(UpdateType.DialogCleared);
        result.ChatId.Should().Be(123);

        // Test typed wrapper
        result.DialogClearedUpdate.Should().NotBeNull();
        result.DialogClearedUpdate!.ChatId.Should().Be(123);
    }

    #endregion

    #region Dialog Removed Tests

    [Fact]
    public void Deserialize_DialogRemoved_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":15,"update_type":"dialog_removed","timestamp":1609459200000,"chat_id":123}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(15);
        result.Type.Should().Be(UpdateType.DialogRemoved);

        // Test typed wrapper
        result.DialogRemovedUpdate.Should().NotBeNull();
    }

    #endregion

    #region Message Chat Created Tests

    [Fact]
    public void Deserialize_MessageChatCreated_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"update_id":16,"update_type":"message_chat_created","timestamp":1609459200000,"user_locale":"ru","message":{"body":{"mid":"mid.123","text":"Chat created"}}}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(16);
        result.Type.Should().Be(UpdateType.MessageChatCreated);

        // Test typed wrapper
        result.MessageChatCreatedUpdate.Should().NotBeNull();
        result.MessageChatCreatedUpdate!.UserLocale.Should().Be("ru");
    }

    #endregion

    #region Unknown Type Tests

    [Fact]
    public void Deserialize_UnknownType_ShouldReturnUnknown()
    {
        // Arrange
        var json = """{"update_id":99,"update_type":"some_future_type","timestamp":1609459200000}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.UpdateId.Should().Be(99);
        result.Type.Should().Be(UpdateType.Unknown);
        result.UpdateTypeRaw.Should().Be("some_future_type");
    }

    [Fact]
    public void Deserialize_MissingUpdateType_ShouldReturnUnknown()
    {
        // Arrange
        var json = """{"update_id":99,"timestamp":1609459200000}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<Update>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(UpdateType.Unknown);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void Serialize_MessageCreated_ShouldSerializeCorrectly()
    {
        // Arrange
        var update = new Update
        {
            UpdateId = 1,
            UpdateTypeRaw = "message_created",
            Timestamp = 1609459200000,
            Message = new Message
            {
                Body = new MessageBody
                {
                    Mid = "mid.123",
                    Text = "Hello"
                }
            }
        };

        // Act
        var json = MaxJsonSerializer.Serialize(update);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"update_id\":1");
        json.Should().Contain("\"update_type\":\"message_created\"");
        json.Should().Contain("\"message\"");
    }

    [Fact]
    public void Serialize_MessageCallback_ShouldSerializeWithCallbackField()
    {
        // Arrange
        var update = new Update
        {
            UpdateId = 2,
            UpdateTypeRaw = "message_callback",
            Callback = new CallbackQuery
            {
                CallbackId = "cb123",
                Payload = "button_clicked"
            }
        };

        // Act
        var json = MaxJsonSerializer.Serialize(update);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"callback\""); // API uses "callback" field
        json.Should().Contain("\"callback_id\":\"cb123\"");
        json.Should().NotContain("\"callback_query\"");
    }

    [Fact]
    public void Serialize_BotAdded_ShouldSerializeAllFields()
    {
        // Arrange
        var update = new Update
        {
            UpdateId = 5,
            UpdateTypeRaw = "bot_added",
            Timestamp = 1609459200000,
            Chat = new Chat { ChatId = 123, Title = "Test" },
            User = new User { Id = 456 },
            InviterId = 789,
            Payload = "promo_summer2025"
        };

        // Act
        var json = MaxJsonSerializer.Serialize(update);

        // Assert
        json.Should().Contain("\"chat\"");
        json.Should().Contain("\"user\"");
        json.Should().Contain("\"inviter_id\":789");
        json.Should().Contain("\"payload\":\"promo_summer2025\"");
    }

    #endregion

    #region Backward Compatibility Tests

    // Legacy update formats are intentionally not supported (strict API mode).

    #endregion
}
