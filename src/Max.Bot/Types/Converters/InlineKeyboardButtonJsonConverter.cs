using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Max.Bot.Types;
using Max.Bot.Types.Enums;

namespace Max.Bot.Types.Converters;

/// <summary>
/// JSON converter for InlineKeyboardButton that handles both new format (type + payload) and legacy format (callback_data).
/// </summary>
public class InlineKeyboardButtonJsonConverter : JsonConverter<InlineKeyboardButton>
{
    /// <inheritdoc />
    public override InlineKeyboardButton Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        var button = new InlineKeyboardButton();

        // Read text (required)
        button.Text = GetString(root, "text") ?? string.Empty;

        // Read type (new format)
        if (root.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String)
        {
            var typeString = typeElement.GetString();
            if (TryParseButtonType(typeString, out var buttonType))
            {
                button.Type = buttonType;
            }
        }

        // Read payload (new format)
        if (root.TryGetProperty("payload", out var payloadElement) && payloadElement.ValueKind != JsonValueKind.Null)
        {
            button.Payload = payloadElement.ValueKind == JsonValueKind.String
                ? payloadElement.GetString()
                : payloadElement.GetRawText();
        }

        // Read url (for link buttons)
        if (GetString(root, "url") is { } url)
        {
            button.Url = url;
            if (button.Type == default(ButtonType) && !string.IsNullOrEmpty(button.Url))
            {
                button.Type = ButtonType.Link;
            }
        }

        // Handle legacy callback_data format (for backward compatibility)
        if (GetString(root, "callback_data") is { } callbackData)
        {
            if (!string.IsNullOrEmpty(callbackData))
            {
                button.Type = ButtonType.Callback;
                button.Payload = callbackData;
            }
        }

        if (root.TryGetProperty("web_app", out var webAppElement) && webAppElement.ValueKind == JsonValueKind.String)
        {
            button.WebApp = webAppElement.GetString();
        }

        if (root.TryGetProperty("contact_id", out var contactIdElement) && contactIdElement.ValueKind == JsonValueKind.Number && contactIdElement.TryGetInt64(out var contactId))
        {
            button.ContactId = contactId;
        }

        if (root.TryGetProperty("quick", out var quickElement) && quickElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            button.Quick = quickElement.GetBoolean();
        }

        button.ChatTitle = GetString(root, "chat_title");
        button.ChatDescription = GetString(root, "chat_description");
        button.StartPayload = GetString(root, "start_payload");
        if (root.TryGetProperty("uuid", out var uuidElement) && uuidElement.ValueKind == JsonValueKind.Number && uuidElement.TryGetInt64(out var uuid))
        {
            button.Uuid = uuid;
        }

        // Read intent (for callback buttons)
        if (root.TryGetProperty("intent", out var intentElement) && intentElement.ValueKind == JsonValueKind.String)
        {
            var intentString = intentElement.GetString();
            if (TryParseButtonIntent(intentString, out var buttonIntent))
            {
                button.Intent = buttonIntent;
            }
        }

        // If type is still not set, default to Callback if payload is present
        if (button.Type == default(ButtonType) && !string.IsNullOrEmpty(button.Payload))
        {
            button.Type = ButtonType.Callback;
        }

        return button;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, InlineKeyboardButton value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write type (required) - convert to snake_case to match API format
        // * OpenApp without web_app keeps the legacy "link" representation (0.6.2 behaviour).
        var typeString = value.Type == ButtonType.OpenApp && string.IsNullOrEmpty(value.WebApp)
            ? "link"
            : ConvertToSnakeCase(value.Type.ToString());
        writer.WriteString("type", typeString);

        // Write text (required)
        writer.WriteString("text", value.Text);

        // Write payload (for callback and message buttons)
        if (!string.IsNullOrEmpty(value.Payload))
        {
            writer.WriteString("payload", value.Payload);
        }

        // Write url (for link buttons)
        if (!string.IsNullOrEmpty(value.Url))
        {
            writer.WriteString("url", value.Url);
        }

        if (!string.IsNullOrEmpty(value.WebApp))
        {
            writer.WriteString("web_app", value.WebApp);
        }

        if (value.ContactId.HasValue)
        {
            writer.WriteNumber("contact_id", value.ContactId.Value);
        }

        if (value.Quick.HasValue)
        {
            writer.WriteBoolean("quick", value.Quick.Value);
        }

        if (value.ChatTitle != null)
        {
            writer.WriteString("chat_title", value.ChatTitle);
        }

        if (value.ChatDescription != null)
        {
            writer.WriteString("chat_description", value.ChatDescription);
        }

        if (value.StartPayload != null)
        {
            writer.WriteString("start_payload", value.StartPayload);
        }

        if (value.Uuid.HasValue)
        {
            writer.WriteNumber("uuid", value.Uuid.Value);
        }

        // Write intent (for callback buttons)
        if (value.Intent.HasValue)
        {
            var intentString = value.Intent.Value.ToString().ToLowerInvariant();
            writer.WriteString("intent", intentString);
        }

        writer.WriteEndObject();
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private static bool TryParseButtonIntent(string? value, out ButtonIntent intent)
    {
        intent = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "default":
                intent = ButtonIntent.Default;
                return true;
            case "positive":
                intent = ButtonIntent.Positive;
                return true;
            case "negative":
                intent = ButtonIntent.Negative;
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseButtonType(string? value, out ButtonType buttonType)
    {
        buttonType = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();

        foreach (var candidate in Enum.GetValues<ButtonType>())
        {
            var serialized = ConvertToSnakeCase(candidate.ToString());
            if (serialized.Equals(normalized, StringComparison.Ordinal))
            {
                buttonType = candidate;
                return true;
            }
        }

        return false;
    }

    private static string ConvertToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}

