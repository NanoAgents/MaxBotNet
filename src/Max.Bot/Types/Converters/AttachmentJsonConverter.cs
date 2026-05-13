using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Max.Bot.Types;

namespace Max.Bot.Types.Converters;

/// <summary>
/// JSON converter for polymorphic deserialization of Attachment objects based on the "type" field.
/// Routes to the correct concrete attachment type.
/// </summary>
public class AttachmentJsonConverter : JsonConverter<Attachment>
{
    /// <inheritdoc />
    public override Attachment? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token.");
        }

        // Parse the JSON document to read properties
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        // Read the type field
        string? typeString = null;
        if (root.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String)
        {
            typeString = typeElement.GetString();
        }

        // Route by type name — primary and most reliable method
        if (IsType(typeString, AttachmentTypeNames.Image))
        {
            return DeserializePhotoAttachment(root, options);
        }

        if (IsType(typeString, AttachmentTypeNames.InlineKeyboard))
        {
            return JsonSerializer.Deserialize<InlineKeyboardAttachment>(root.GetRawText(), options);
        }

        if (IsType(typeString, AttachmentTypeNames.Location))
        {
            return JsonSerializer.Deserialize<LocationAttachment>(root.GetRawText(), options);
        }

        if (IsType(typeString, AttachmentTypeNames.Contact))
        {
            return DeserializeContactAttachment(root, options);
        }

        if (IsType(typeString, AttachmentTypeNames.Video))
        {
            return DeserializeMediaAttachment<VideoAttachment>(root, "video", options);
        }

        if (IsType(typeString, AttachmentTypeNames.Audio))
        {
            return DeserializeMediaAttachment<AudioAttachment>(root, "audio", options);
        }

        if (IsType(typeString, AttachmentTypeNames.File))
        {
            return DeserializeMediaAttachment<DocumentAttachment>(root, "document", options);
        }

        // Fallback for unknown types — use DocumentAttachment as it has the most generic fields
        return JsonSerializer.Deserialize<DocumentAttachment>(root.GetRawText(), options);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Attachment value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case PhotoAttachment photoAttachment:
                JsonSerializer.Serialize(writer, photoAttachment, options);
                break;
            case VideoAttachment videoAttachment:
                JsonSerializer.Serialize(writer, videoAttachment, options);
                break;
            case AudioAttachment audioAttachment:
                JsonSerializer.Serialize(writer, audioAttachment, options);
                break;
            case DocumentAttachment documentAttachment:
                JsonSerializer.Serialize(writer, documentAttachment, options);
                break;
            case LocationAttachment locationAttachment:
                JsonSerializer.Serialize(writer, locationAttachment, options);
                break;
            case ContactAttachment contactAttachment:
                JsonSerializer.Serialize(writer, contactAttachment, options);
                break;
            case InlineKeyboardAttachment inlineKeyboardAttachment:
                JsonSerializer.Serialize(writer, inlineKeyboardAttachment, options);
                break;
            default:
                throw new JsonException($"Unknown attachment type: {value.GetType()}");
        }
    }

    private static T? DeserializeAttachment<T>(JsonElement root, string payloadPropertyName, JsonSerializerOptions options)
        where T : Attachment
    {
        if (root.TryGetProperty(payloadPropertyName, out var payload) && payload.ValueKind == JsonValueKind.Object)
        {
            var attachment = JsonSerializer.Deserialize<T>(payload.GetRawText(), options);
            if (attachment != null && root.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String)
            {
                attachment.Type = typeElement.GetString() ?? attachment.Type;
            }

            return attachment;
        }

        return JsonSerializer.Deserialize<T>(root.GetRawText(), options);
    }

    private static PhotoAttachment? DeserializePhotoAttachment(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("payload", out var payload) && payload.ValueKind == JsonValueKind.Object)
        {
            var attachment = new PhotoAttachment();

            if (payload.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt64(out var id))
            {
                attachment.Id = id;
            }
            else if (payload.TryGetProperty("photo_id", out var photoIdElement) && photoIdElement.ValueKind == JsonValueKind.Number && photoIdElement.TryGetInt64(out var photoId))
            {
                attachment.Id = photoId;
            }

            if (payload.TryGetProperty("file_id", out var fileIdElement) && fileIdElement.ValueKind == JsonValueKind.String)
            {
                attachment.FileId = fileIdElement.GetString() ?? string.Empty;
            }
            else if (payload.TryGetProperty("token", out var tokenElement) && tokenElement.ValueKind == JsonValueKind.String)
            {
                attachment.FileId = tokenElement.GetString() ?? string.Empty;
            }

            if (payload.TryGetProperty("width", out var widthElement) && widthElement.ValueKind == JsonValueKind.Number && widthElement.TryGetInt32(out var width))
            {
                attachment.Width = width;
            }

            if (payload.TryGetProperty("height", out var heightElement) && heightElement.ValueKind == JsonValueKind.Number && heightElement.TryGetInt32(out var height))
            {
                attachment.Height = height;
            }

            if (payload.TryGetProperty("file_size", out var fileSizeElement) && fileSizeElement.ValueKind == JsonValueKind.Number && fileSizeElement.TryGetInt64(out var fileSize))
            {
                attachment.FileSize = fileSize;
            }

            if (payload.TryGetProperty("url", out var urlElement) && urlElement.ValueKind == JsonValueKind.String)
            {
                attachment.Url = urlElement.GetString();
            }

            return attachment;
        }

        if (root.TryGetProperty("photo", out var photo) && photo.ValueKind == JsonValueKind.Object)
        {
            var attachment = JsonSerializer.Deserialize<PhotoAttachment>(photo.GetRawText(), options);
            if (attachment != null)
            {
                attachment.Type = AttachmentTypeNames.Image;
            }

            return attachment;
        }

        return JsonSerializer.Deserialize<PhotoAttachment>(root.GetRawText(), options);
    }

    private static ContactAttachment? DeserializeContactAttachment(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("payload", out var payload) && payload.ValueKind == JsonValueKind.Object)
        {
            var attachment = JsonSerializer.Deserialize<ContactAttachment>(payload.GetRawText(), options);
            if (attachment != null)
            {
                // Ensure type is always set even when payload does not include it.
                attachment.Type = AttachmentTypeNames.Contact;
            }
            return attachment;
        }

        return JsonSerializer.Deserialize<ContactAttachment>(root.GetRawText(), options);
    }

    private static T? DeserializeMediaAttachment<T>(JsonElement root, string payloadPropertyName, JsonSerializerOptions options)
        where T : Attachment
    {
        if (root.TryGetProperty("payload", out var payload) && payload.ValueKind == JsonValueKind.Object)
        {
            var attachment = JsonSerializer.Deserialize<T>(payload.GetRawText(), options);
            if (attachment == null)
            {
                return null;
            }

            switch (attachment)
            {
                case AudioAttachment audio when string.IsNullOrWhiteSpace(audio.FileId)
                    && payload.TryGetProperty("token", out var audioToken)
                    && audioToken.ValueKind == JsonValueKind.String:
                    audio.FileId = audioToken.GetString() ?? string.Empty;
                    break;
                case VideoAttachment video when string.IsNullOrWhiteSpace(video.FileId)
                    && payload.TryGetProperty("token", out var videoToken)
                    && videoToken.ValueKind == JsonValueKind.String:
                    video.FileId = videoToken.GetString() ?? string.Empty;
                    break;
                case DocumentAttachment document when string.IsNullOrWhiteSpace(document.FileId)
                    && payload.TryGetProperty("token", out var documentToken)
                    && documentToken.ValueKind == JsonValueKind.String:
                    document.FileId = documentToken.GetString() ?? string.Empty;
                    break;
            }

            return attachment;
        }

        return DeserializeAttachment<T>(root, payloadPropertyName, options);
    }

    private static bool IsType(string? actualType, string expectedType)
    {
        return !string.IsNullOrWhiteSpace(actualType) &&
               actualType.Equals(expectedType, StringComparison.OrdinalIgnoreCase);
    }
}
