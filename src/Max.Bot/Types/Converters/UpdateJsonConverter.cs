using System.Text.Json;
using System.Text.Json.Serialization;
using Max.Bot.Types;

namespace Max.Bot.Types.Converters;

/// <summary>
/// JSON converter for Update that handles all update types from Max API.
/// </summary>
public class UpdateJsonConverter : JsonConverter<Update>
{
    /// <inheritdoc />
    public override Update Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null!;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token.");
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var update = new Update();

        // Read base fields present in all update types
        if (root.TryGetProperty("update_id", out var updateIdElement))
        {
            update.UpdateId = updateIdElement.GetInt64();
        }

        if (root.TryGetProperty("update_type", out var updateTypeElement))
        {
            update.UpdateTypeRaw = updateTypeElement.GetString();
        }

        if (root.TryGetProperty("timestamp", out var timestampElement))
        {
            update.Timestamp = timestampElement.GetInt64();
        }

        if (root.TryGetProperty("user_locale", out var userLocaleElement))
        {
            update.UserLocale = userLocaleElement.GetString();
        }

        // Read message (message_created, message_edited, message_removed, message_chat_created)
        if (root.TryGetProperty("message", out var messageElement))
        {
            update.Message = JsonSerializer.Deserialize<Message>(messageElement.GetRawText(), options);
        }

        // Read callback (message_callback)
        if (root.TryGetProperty("callback", out var callbackElement))
        {
            update.Callback = JsonSerializer.Deserialize<CallbackQuery>(callbackElement.GetRawText(), options);
        }

        // Read chat (bot_added, bot_removed, user_added, user_removed, chat_title_changed)
        if (root.TryGetProperty("chat", out var chatElement))
        {
            update.Chat = JsonSerializer.Deserialize<Chat>(chatElement.GetRawText(), options);
        }

        // Read user (bot_added, bot_removed, bot_started, bot_stopped, user_added, user_removed)
        if (root.TryGetProperty("user", out var userElement))
        {
            update.User = JsonSerializer.Deserialize<User>(userElement.GetRawText(), options);
        }

        // Read inviter_id (bot_added, user_added)
        if (root.TryGetProperty("inviter_id", out var inviterIdElement))
        {
            update.InviterId = inviterIdElement.GetInt64();
        }

        // Read chat_id (dialog_muted, dialog_unmuted, dialog_cleared, dialog_removed, bot_started, bot_stopped)
        if (root.TryGetProperty("chat_id", out var chatIdElement))
        {
            update.ChatId = chatIdElement.GetInt64();
        }

        // Read is_muted (dialog_muted, dialog_unmuted)
        if (root.TryGetProperty("is_muted", out var isMutedElement))
        {
            update.IsMuted = isMutedElement.GetBoolean();
        }

        if (root.TryGetProperty("payload", out var payload))
        {
            update.Payload = payload.GetString();
        }

        return update;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Update value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("update_id", value.UpdateId);

        if (value.UpdateTypeRaw != null)
        {
            writer.WriteString("update_type", value.UpdateTypeRaw);
        }

        if (value.Timestamp.HasValue)
        {
            writer.WriteNumber("timestamp", value.Timestamp.Value);
        }

        if (value.UserLocale != null)
        {
            writer.WriteString("user_locale", value.UserLocale);
        }

        // Write message
        if (value.Message != null)
        {
            writer.WritePropertyName("message");
            JsonSerializer.Serialize(writer, value.Message, options);
        }

        // Write callback (API format uses "callback" field)
        if (value.Callback != null)
        {
            writer.WritePropertyName("callback");
            JsonSerializer.Serialize(writer, value.Callback, options);
        }

        // Write chat
        if (value.Chat != null)
        {
            writer.WritePropertyName("chat");
            JsonSerializer.Serialize(writer, value.Chat, options);
        }

        // Write user
        if (value.User != null)
        {
            writer.WritePropertyName("user");
            JsonSerializer.Serialize(writer, value.User, options);
        }

        // Write inviter_id
        if (value.InviterId.HasValue)
        {
            writer.WriteNumber("inviter_id", value.InviterId.Value);
        }

        // Write chat_id
        if (value.ChatId.HasValue)
        {
            writer.WriteNumber("chat_id", value.ChatId.Value);
        }

        // Write is_muted
        if (value.IsMuted.HasValue)
        {
            writer.WriteBoolean("is_muted", value.IsMuted.Value);
        }

        writer.WriteEndObject();
    }
}
