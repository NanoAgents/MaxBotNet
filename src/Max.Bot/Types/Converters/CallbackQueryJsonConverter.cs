using System.Text.Json;
using System.Text.Json.Serialization;
using Max.Bot.Types;

namespace Max.Bot.Types.Converters;

/// <summary>
/// JSON converter for CallbackQuery that handles API format (user, payload, callback_id, timestamp, message).
/// </summary>
public class CallbackQueryJsonConverter : JsonConverter<CallbackQuery>
{
    /// <inheritdoc />
    public override CallbackQuery Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        var callbackQuery = new CallbackQuery();

        // Read callback_id (API format)
        if (root.TryGetProperty("callback_id", out var callbackIdElement))
        {
            callbackQuery.CallbackId = callbackIdElement.GetString();
        }

        // Read user (API format)
        if (root.TryGetProperty("user", out var userElement))
        {
            callbackQuery.User = JsonSerializer.Deserialize<User>(userElement.GetRawText(), options);
        }

        // Read payload (API format)
        if (root.TryGetProperty("payload", out var payloadElement))
        {
            callbackQuery.Payload = payloadElement.ValueKind == JsonValueKind.String
                ? payloadElement.GetString()
                : payloadElement.GetRawText();
        }

        // Read timestamp
        if (root.TryGetProperty("timestamp", out var timestampElement))
        {
            callbackQuery.Timestamp = timestampElement.GetInt64();
        }

        return callbackQuery;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, CallbackQuery value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write callback_id (API format)
        if (value.CallbackId != null)
        {
            writer.WriteString("callback_id", value.CallbackId);
        }

        // Write user (API format)
        if (value.User != null)
        {
            writer.WritePropertyName("user");
            JsonSerializer.Serialize(writer, value.User, options);
        }

        // Write payload (API format)
        if (value.Payload != null)
        {
            writer.WriteString("payload", value.Payload);
        }

        // Write timestamp if present
        if (value.Timestamp.HasValue)
        {
            writer.WriteNumber("timestamp", value.Timestamp.Value);
        }

        writer.WriteEndObject();
    }
}

