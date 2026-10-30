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
        if (root.TryGetProperty("update_id", out var updateIdElement) && updateIdElement.ValueKind == JsonValueKind.Number)
        {
            update.UpdateId = updateIdElement.GetInt64();
        }

        if (root.TryGetProperty("update_type", out var updateTypeElement))
        {
            update.UpdateTypeRaw = updateTypeElement.GetString();
        }

        update.Timestamp = GetInt64(root, "timestamp") ?? 0;

        update.UserLocale = GetString(root, "user_locale");

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
        update.InviterId = GetInt64(root, "inviter_id");

        // Read chat_id (bot_added, bot_removed, bot_started, bot_stopped, user_added, user_removed,
        // chat_title_changed, message_removed, dialog_* events)
        update.ChatId = GetInt64(root, "chat_id");

        // Read is_muted (dialog_muted, dialog_unmuted)
        if (root.TryGetProperty("is_muted", out var isMutedElement) &&
            isMutedElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            update.IsMuted = isMutedElement.GetBoolean();
        }

        // Read message_id / user_id (message_removed, message_chat_created)
        update.MessageId = GetString(root, "message_id");
        update.UserId = GetInt64(root, "user_id");

        // Read admin_id (user_removed), title (chat_title_changed, message_chat_created), is_channel, muted_until
        update.AdminId = GetInt64(root, "admin_id");
        update.Title = GetString(root, "title");
        update.MutedUntil = GetInt64(root, "muted_until");
        if (root.TryGetProperty("is_channel", out var isChannelElement) &&
            isChannelElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            update.IsChannel = isChannelElement.GetBoolean();
        }

        // Read deep-link payload (bot_started: payload, message_chat_created: start_payload)
        if (root.TryGetProperty("payload", out var payloadElement) && payloadElement.ValueKind != JsonValueKind.Null)
        {
            update.Payload = payloadElement.ValueKind == JsonValueKind.String ? payloadElement.GetString() : payloadElement.GetRawText();
        }
        else if (root.TryGetProperty("start_payload", out var startPayloadElement) && startPayloadElement.ValueKind == JsonValueKind.String)
        {
            update.Payload = startPayloadElement.GetString();
        }

        InferChatFromMessage(update);

        return update;
    }

    /// <summary>
    /// Message updates (message_created, message_edited, message_callback) carry no chat object:
    /// MAX sends the chat identifier and type in <c>message.recipient</c>. Expose them via
    /// <see cref="Update.ChatId"/> and a partial <see cref="Update.Chat"/>.
    /// </summary>
    private static void InferChatFromMessage(Update update)
    {
        if (update.ChatId == null && update.Chat != null)
        {
            // message_chat_created carries a full chat object but no chat_id.
            update.SetInferredChat(update.Chat.ChatId, null);
        }

        var recipient = update.Message?.Recipient;
        if (recipient?.ChatId is not { } recipientChatId)
        {
            return;
        }

        var inferredChatId = update.ChatId == null ? recipientChatId : (long?)null;
        Chat? inferredChat = null;
        if (update.Chat == null && recipient.ChatType is { } chatType)
        {
            inferredChat = new Chat
            {
                ChatId = recipientChatId,
                Type = chatType
            };
        }

        update.SetInferredChat(inferredChatId, inferredChat);
    }

    private static long? GetInt64(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt64(out var value) => value,
            JsonValueKind.String when long.TryParse(element.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null
        };
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

        writer.WriteNumber("timestamp", value.Timestamp);

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

        // Write chat (skip the partial chat inferred from message.recipient)
        if (value.Chat != null && !value.IsChatInferred)
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

        // Write chat_id (skip the value inferred from message.recipient)
        if (value.ChatId.HasValue && !value.IsChatIdInferred)
        {
            writer.WriteNumber("chat_id", value.ChatId.Value);
        }

        if (value.MessageId != null)
        {
            writer.WriteString("message_id", value.MessageId);
        }

        if (value.UserId.HasValue)
        {
            writer.WriteNumber("user_id", value.UserId.Value);
        }

        if (value.AdminId.HasValue)
        {
            writer.WriteNumber("admin_id", value.AdminId.Value);
        }

        if (value.Title != null)
        {
            writer.WriteString("title", value.Title);
        }

        if (value.IsChannel.HasValue)
        {
            writer.WriteBoolean("is_channel", value.IsChannel.Value);
        }

        if (value.MutedUntil.HasValue)
        {
            writer.WriteNumber("muted_until", value.MutedUntil.Value);
        }

        // Write is_muted
        if (value.IsMuted.HasValue)
        {
            writer.WriteBoolean("is_muted", value.IsMuted.Value);
        }

        if (value.Payload != null)
        {
            writer.WriteString("payload", value.Payload);
        }

        writer.WriteEndObject();
    }
}
