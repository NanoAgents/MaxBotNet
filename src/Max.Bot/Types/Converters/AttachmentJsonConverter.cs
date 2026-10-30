using System;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        if (IsType(typeString, AttachmentTypeNames.InlineKeyboard))
        {
            return DeserializeInlineKeyboard(root, options);
        }

        if (IsType(typeString, AttachmentTypeNames.Image))
        {
            return DeserializeMerged<PhotoAttachment>(root, typeString, "photo", options);
        }

        if (IsType(typeString, AttachmentTypeNames.Video))
        {
            return DeserializeMerged<VideoAttachment>(root, typeString, "video", options);
        }

        if (IsType(typeString, AttachmentTypeNames.Audio))
        {
            return DeserializeMerged<AudioAttachment>(root, typeString, "audio", options);
        }

        if (IsType(typeString, AttachmentTypeNames.File))
        {
            return DeserializeMerged<DocumentAttachment>(root, typeString, "document", options);
        }

        if (IsType(typeString, AttachmentTypeNames.Contact))
        {
            return DeserializeMerged<ContactAttachment>(root, typeString, null, options);
        }

        if (IsType(typeString, AttachmentTypeNames.Location))
        {
            return DeserializeMerged<LocationAttachment>(root, typeString, null, options);
        }

        if (IsType(typeString, AttachmentTypeNames.Sticker))
        {
            return DeserializeMerged<StickerAttachment>(root, typeString, null, options);
        }

        if (IsType(typeString, AttachmentTypeNames.Share))
        {
            return DeserializeMerged<ShareAttachment>(root, typeString, null, options);
        }

        if (IsType(typeString, AttachmentTypeNames.Data))
        {
            return DeserializeMerged<DataAttachment>(root, typeString, null, options);
        }

        if (IsType(typeString, AttachmentTypeNames.ReplyKeyboard))
        {
            return DeserializeMerged<ReplyKeyboardAttachment>(root, typeString, null, options);
        }

        // Types unknown to this library version keep their raw JSON.
        return new UnknownAttachment
        {
            Type = typeString ?? string.Empty,
            RawJson = root.GetRawText()
        };
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
            case StickerAttachment stickerAttachment:
                JsonSerializer.Serialize(writer, stickerAttachment, options);
                break;
            case ShareAttachment shareAttachment:
                JsonSerializer.Serialize(writer, shareAttachment, options);
                break;
            case DataAttachment dataAttachment:
                JsonSerializer.Serialize(writer, dataAttachment, options);
                break;
            case ReplyKeyboardAttachment replyKeyboardAttachment:
                JsonSerializer.Serialize(writer, replyKeyboardAttachment, options);
                break;
            case UnknownAttachment unknownAttachment:
                using (var document = JsonDocument.Parse(unknownAttachment.RawJson))
                {
                    document.RootElement.WriteTo(writer);
                }

                break;
            default:
                throw new JsonException($"Unknown attachment type: {value.GetType()}");
        }
    }

    /// <summary>
    /// Inline keyboard keeps its buttons nested in <c>payload</c>, so it is deserialized as is (without merging).
    /// A malformed keyboard yields an empty <see cref="InlineKeyboardAttachment"/> instead of breaking the whole message.
    /// </summary>
    private static InlineKeyboardAttachment DeserializeInlineKeyboard(JsonElement root, JsonSerializerOptions options)
    {
        InlineKeyboardAttachment? attachment;
        try
        {
            attachment = root.Deserialize<InlineKeyboardAttachment>(options);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            attachment = null;
        }

        attachment ??= new InlineKeyboardAttachment();
        attachment.Payload ??= new InlineKeyboard();
        attachment.Type = AttachmentTypeNames.InlineKeyboard;
        return attachment;
    }

    /// <summary>
    /// MAX API splits attachment data between the <c>payload</c> object (url, token, photo_id, vcf_info, ...)
    /// and the attachment root (filename, size, width, height, duration, thumbnail, ...).
    /// This method merges both levels into one object and deserializes it into the flat attachment model.
    /// </summary>
    private static T? DeserializeMerged<T>(JsonElement root, string? typeString, string? legacyPayloadPropertyName, JsonSerializerOptions options)
        where T : Attachment, new()
    {
        try
        {
            return DeserializeMergedCore<T>(root, typeString, legacyPayloadPropertyName, options);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            // An attachment with unexpected field types must not break the whole message/update batch.
            var fallback = new T();
            if (!string.IsNullOrWhiteSpace(typeString))
            {
                fallback.Type = typeString;
            }

            return fallback;
        }
    }

    private static T? DeserializeMergedCore<T>(JsonElement root, string? typeString, string? legacyPayloadPropertyName, JsonSerializerOptions options)
        where T : Attachment
    {
        var merged = new JsonObject();

        JsonElement payload = default;
        var hasPayload = root.TryGetProperty("payload", out payload) && payload.ValueKind == JsonValueKind.Object;
        if (!hasPayload && legacyPayloadPropertyName != null)
        {
            // Legacy nested format, e.g. {"type":"image","photo":{...}}
            hasPayload = root.TryGetProperty(legacyPayloadPropertyName, out payload) && payload.ValueKind == JsonValueKind.Object;
        }

        if (hasPayload)
        {
            foreach (var property in payload.EnumerateObject())
            {
                AddProperty(merged, property);
            }
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("payload") ||
                (legacyPayloadPropertyName != null && property.NameEquals(legacyPayloadPropertyName) && property.Value.ValueKind == JsonValueKind.Object))
            {
                continue;
            }

            AddProperty(merged, property);
        }

        // Map MAX API field names onto the model property names.
        CopyAlias(merged, "photo_id", "id");
        CopyAlias(merged, "filename", "file_name");
        CopyAlias(merged, "size", "file_size");

        if (merged.TryGetPropertyValue("file_id", out var fileIdNode) && fileIdNode is JsonValue fileIdValue && fileIdValue.TryGetValue<long>(out var numericFileId))
        {
            merged["file_id"] = numericFileId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var attachment = merged.Deserialize<T>(options);
        if (attachment == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(typeString))
        {
            attachment.Type = typeString;
        }

        // Backward compatibility: FileId historically carried the media token.
        switch (attachment)
        {
            case PhotoAttachment photo when string.IsNullOrEmpty(photo.FileId) && !string.IsNullOrEmpty(photo.Token):
                photo.FileId = photo.Token;
                break;
            case VideoAttachment video when string.IsNullOrEmpty(video.FileId) && !string.IsNullOrEmpty(video.Token):
                video.FileId = video.Token;
                break;
            case AudioAttachment audio when string.IsNullOrEmpty(audio.FileId) && !string.IsNullOrEmpty(audio.Token):
                audio.FileId = audio.Token;
                break;
            case DocumentAttachment document when string.IsNullOrEmpty(document.FileId) && !string.IsNullOrEmpty(document.Token):
                document.FileId = document.Token;
                break;
        }

        return attachment;
    }

    private static void AddProperty(JsonObject target, JsonProperty property)
    {
        // Skip nulls so that non-nullable model properties keep their defaults.
        if (property.Value.ValueKind == JsonValueKind.Null || target.ContainsKey(property.Name))
        {
            return;
        }

        target[property.Name] = JsonNode.Parse(property.Value.GetRawText());
    }

    private static void CopyAlias(JsonObject target, string sourceName, string targetName)
    {
        if (!target.ContainsKey(targetName) && target.TryGetPropertyValue(sourceName, out var value) && value != null)
        {
            target[targetName] = value.DeepClone();
        }
    }

    private static bool IsType(string? actualType, string expectedType)
    {
        return !string.IsNullOrWhiteSpace(actualType) &&
               actualType.Equals(expectedType, StringComparison.OrdinalIgnoreCase);
    }
}
