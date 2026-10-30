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
        if (root.TryGetProperty("callback_id", out var callbackIdElement) && callbackIdElement.ValueKind == JsonValueKind.String)
        {
            callbackQuery.CallbackId = callbackIdElement.GetString() ?? string.Empty;
        }

        // Read user (API format)
        if (root.TryGetProperty("user", out var userElement) && userElement.ValueKind == JsonValueKind.Object)
        {
            callbackQuery.User = userElement.Deserialize<User>(options) ?? new User();
        }

        // Read payload (API format)
        if (root.TryGetProperty("payload", out var payloadElement) && payloadElement.ValueKind != JsonValueKind.Null)
        {
            callbackQuery.Payload = payloadElement.ValueKind == JsonValueKind.String
                ? payloadElement.GetString()
                : payloadElement.GetRawText();
        }

        // Read timestamp
        if (root.TryGetProperty("timestamp", out var timestampElement) &&
            timestampElement.ValueKind == JsonValueKind.Number &&
            timestampElement.TryGetInt64(out var timestamp))
        {
            callbackQuery.Timestamp = timestamp;
        }

        return callbackQuery;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, CallbackQuery value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write callback_id (API format)
        writer.WriteString("callback_id", value.CallbackId ?? string.Empty);

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

        writer.WriteNumber("timestamp", value.Timestamp);

        writer.WriteEndObject();
    }
}

