using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using Max.Bot.Types.Enums;

namespace Max.Bot.Types.Requests;

/// <summary>
/// Represents a request to create an attachment for a message.
/// </summary>
/// <remarks>
/// Use the static factory methods (<see cref="ImageFromUrl"/>, <see cref="Image(string)"/>, <see cref="FromUpload"/>,
/// <see cref="Video"/>, <see cref="File"/>, <see cref="Location"/> and others) to build payloads in the format expected by MAX API.
/// </remarks>
public class AttachmentRequest
{
    /// <summary>
    /// Gets or sets the type of the attachment.
    /// </summary>
    /// <value>The type of the attachment (image, video, audio, file, sticker, contact, share, location or inline_keyboard).</value>
    [Required(ErrorMessage = "Attachment type is required.")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payload of the attachment.
    /// </summary>
    /// <value>
    /// The payload object containing attachment data (e.g., <c>{"token": "..."}</c> for uploaded media,
    /// <c>{"url": "..."}</c> for an image by URL). Not used for <c>location</c> attachments.
    /// </value>
    [JsonPropertyName("payload")]
    public object? Payload { get; set; }

    /// <summary>
    /// Gets or sets the latitude for <c>location</c> attachments.
    /// </summary>
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude for <c>location</c> attachments.
    /// </summary>
    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    /// <summary>
    /// Gets or sets the buttons of a <c>reply_keyboard</c> attachment (rows of buttons). Use <see cref="ReplyKeyboard"/> to create it.
    /// </summary>
    [JsonPropertyName("buttons")]
    public ReplyKeyboardButton[][]? Buttons { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a <c>reply_keyboard</c> is shown in a group chat only to the user
    /// who mentioned the bot or replied to it.
    /// </summary>
    [JsonPropertyName("direct")]
    public bool? Direct { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the only chat participant who sees a <c>reply_keyboard</c>.
    /// </summary>
    [JsonPropertyName("direct_user_id")]
    public long? DirectUserId { get; set; }

    /// <summary>
    /// Creates an image attachment from any public image URL. MAX downloads the image itself.
    /// </summary>
    /// <param name="url">The absolute URL of the image.</param>
    /// <returns>The attachment request.</returns>
    /// <exception cref="ArgumentException">Thrown when url is null, empty or not an absolute URL.</exception>
    public static AttachmentRequest ImageFromUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Image URL must be an absolute URL.", nameof(url));
        }

        return new AttachmentRequest
        {
            Type = AttachmentTypeNames.Image,
            Payload = new ImageAttachmentRequestPayload { Url = url }
        };
    }

    /// <summary>
    /// Creates an image attachment from the token of an uploaded or previously sent image.
    /// </summary>
    /// <param name="token">The image token.</param>
    /// <returns>The attachment request.</returns>
    public static AttachmentRequest Image(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return new AttachmentRequest
        {
            Type = AttachmentTypeNames.Image,
            Payload = new ImageAttachmentRequestPayload { Token = token }
        };
    }

    /// <summary>
    /// Creates an image attachment from the result of an image upload.
    /// Uses the <c>photos</c> map when available, otherwise the token.
    /// </summary>
    /// <param name="upload">The upload result returned by <c>IFilesApi.UploadAsync</c> or <c>IFilesApi.UploadFileDataAsync</c>.</param>
    /// <returns>The attachment request.</returns>
    /// <exception cref="ArgumentException">Thrown when the upload result contains neither photos nor a token.</exception>
    public static AttachmentRequest Image(FileUploadResult upload)
    {
        ArgumentNullException.ThrowIfNull(upload);

        if (upload.Photos is { Count: > 0 })
        {
            return new AttachmentRequest
            {
                Type = AttachmentTypeNames.Image,
                Payload = new ImageAttachmentRequestPayload { Photos = upload.Photos }
            };
        }

        if (string.IsNullOrWhiteSpace(upload.Token))
        {
            throw new ArgumentException("Upload result does not contain photo tokens.", nameof(upload));
        }

        return Image(upload.Token);
    }

    /// <summary>
    /// Creates a video attachment from an uploaded video token.
    /// </summary>
    /// <param name="token">The video token (<see cref="UploadResponse.Token"/> or <see cref="FileUploadResult.Token"/>).</param>
    /// <returns>The attachment request.</returns>
    public static AttachmentRequest Video(string token) => CreateTokenAttachment(AttachmentTypeNames.Video, token);

    /// <summary>
    /// Creates an audio attachment from an uploaded audio token.
    /// </summary>
    /// <param name="token">The audio token (<see cref="UploadResponse.Token"/> or <see cref="FileUploadResult.Token"/>).</param>
    /// <returns>The attachment request.</returns>
    public static AttachmentRequest Audio(string token) => CreateTokenAttachment(AttachmentTypeNames.Audio, token);

    /// <summary>
    /// Creates a file attachment from an uploaded file token.
    /// </summary>
    /// <param name="token">The file token returned after the upload.</param>
    /// <returns>The attachment request.</returns>
    public static AttachmentRequest File(string token) => CreateTokenAttachment(AttachmentTypeNames.File, token);

    /// <summary>
    /// Creates an attachment of the matching type from an upload result.
    /// </summary>
    /// <param name="uploadType">The type used when uploading the file.</param>
    /// <param name="upload">The upload result.</param>
    /// <returns>The attachment request.</returns>
    /// <exception cref="ArgumentException">Thrown when the upload result does not contain a token.</exception>
    public static AttachmentRequest FromUpload(UploadType uploadType, FileUploadResult upload)
    {
        ArgumentNullException.ThrowIfNull(upload);

        if (uploadType == UploadType.Image)
        {
            return Image(upload);
        }

        if (string.IsNullOrWhiteSpace(upload.Token))
        {
            throw new ArgumentException(
                "Upload result does not contain a token. For video and audio use IFilesApi.UploadAsync or UploadResponse.Token.",
                nameof(upload));
        }

        return uploadType switch
        {
            UploadType.Video => Video(upload.Token),
            UploadType.Audio => Audio(upload.Token),
            UploadType.File => File(upload.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(uploadType), uploadType, "Unknown upload type.")
        };
    }

    /// <summary>
    /// Creates a sticker attachment.
    /// </summary>
    /// <param name="code">The sticker code.</param>
    /// <returns>The attachment request.</returns>
    public static AttachmentRequest Sticker(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new AttachmentRequest
        {
            Type = AttachmentTypeNames.Sticker,
            Payload = new StickerAttachmentRequestPayload { Code = code }
        };
    }

    /// <summary>
    /// Creates a contact attachment.
    /// </summary>
    /// <param name="name">The contact name.</param>
    /// <param name="contactId">The MAX user identifier of the contact. Optional.</param>
    /// <param name="vcfInfo">Full contact information in VCF format. Optional.</param>
    /// <param name="vcfPhone">The contact phone in VCF format. Optional.</param>
    /// <returns>The attachment request.</returns>
    public static AttachmentRequest Contact(string? name = null, long? contactId = null, string? vcfInfo = null, string? vcfPhone = null)
    {
        if (string.IsNullOrWhiteSpace(name) && contactId == null && string.IsNullOrWhiteSpace(vcfInfo) && string.IsNullOrWhiteSpace(vcfPhone))
        {
            throw new ArgumentException("At least one of name, contactId, vcfInfo or vcfPhone must be provided.");
        }

        return new AttachmentRequest
        {
            Type = AttachmentTypeNames.Contact,
            Payload = new ContactAttachmentRequestPayload
            {
                Name = name,
                ContactId = contactId,
                VcfInfo = vcfInfo,
                VcfPhone = vcfPhone
            }
        };
    }

    /// <summary>
    /// Creates a location attachment.
    /// </summary>
    /// <param name="latitude">The latitude.</param>
    /// <param name="longitude">The longitude.</param>
    /// <returns>The attachment request.</returns>
    public static AttachmentRequest Location(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180.");
        }

        return new AttachmentRequest
        {
            Type = AttachmentTypeNames.Location,
            Latitude = latitude,
            Longitude = longitude
        };
    }

    /// <summary>
    /// Creates a link preview (share) attachment.
    /// </summary>
    /// <param name="url">The URL to share. Optional if token is provided.</param>
    /// <param name="token">The token of an existing share attachment. Optional if url is provided.</param>
    /// <returns>The attachment request.</returns>
    public static AttachmentRequest Share(string? url = null, string? token = null)
    {
        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Either url or token must be provided.");
        }

        return new AttachmentRequest
        {
            Type = AttachmentTypeNames.Share,
            Payload = new ShareAttachmentRequestPayload { Url = url, Token = token }
        };
    }

    /// <summary>
    /// Creates an inline keyboard attachment.
    /// </summary>
    /// <param name="keyboard">The keyboard.</param>
    /// <returns>The attachment request.</returns>
    public static AttachmentRequest Keyboard(InlineKeyboard keyboard)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        var sourceRows = keyboard.Buttons ?? Array.Empty<InlineKeyboardButton[]>();
        var normalizedRows = sourceRows.Select(row => row ?? Array.Empty<InlineKeyboardButton>()).ToArray();

        return new AttachmentRequest
        {
            Type = AttachmentTypeNames.InlineKeyboard,
            Payload = new InlineKeyboard(normalizedRows)
        };
    }

    /// <summary>
    /// Creates a reply keyboard attachment (<c>reply_keyboard</c>): buttons shown instead of the user's keyboard.
    /// </summary>
    /// <param name="buttons">The rows of buttons.</param>
    /// <param name="direct"><c>true</c> to show the keyboard in a group chat only to the user who mentioned the bot or replied to it.</param>
    /// <param name="directUserId">The identifier of the only chat participant who sees the keyboard.</param>
    /// <returns>The attachment request.</returns>
    public static AttachmentRequest ReplyKeyboard(ReplyKeyboardButton[][] buttons, bool? direct = null, long? directUserId = null)
    {
        ArgumentNullException.ThrowIfNull(buttons);

        var rows = buttons.Select(row => row ?? Array.Empty<ReplyKeyboardButton>()).Where(row => row.Length > 0).ToArray();
        if (rows.Length == 0)
        {
            throw new ArgumentException("Reply keyboard must contain at least one button.", nameof(buttons));
        }

        return new AttachmentRequest
        {
            Type = AttachmentTypeNames.ReplyKeyboard,
            Buttons = rows,
            Direct = direct,
            DirectUserId = directUserId
        };
    }

    private static AttachmentRequest CreateTokenAttachment(string type, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return new AttachmentRequest
        {
            Type = type,
            Payload = new TokenAttachmentRequestPayload { Token = token }
        };
    }
}

/// <summary>
/// Payload of an <c>image</c> attachment request. Only one of the properties should be set.
/// </summary>
public class ImageAttachmentRequestPayload
{
    /// <summary>
    /// Gets or sets any external image URL to attach.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the token of an existing image attachment.
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the tokens obtained after uploading images.
    /// </summary>
    [JsonPropertyName("photos")]
    public Dictionary<string, PhotoSizeToken>? Photos { get; set; }
}

/// <summary>
/// Payload of <c>video</c>, <c>audio</c> and <c>file</c> attachment requests.
/// </summary>
public class TokenAttachmentRequestPayload
{
    /// <summary>
    /// Gets or sets the uploaded media token.
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Payload of a <c>sticker</c> attachment request.
/// </summary>
public class StickerAttachmentRequestPayload
{
    /// <summary>
    /// Gets or sets the sticker code.
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Payload of a <c>contact</c> attachment request.
/// </summary>
public class ContactAttachmentRequestPayload
{
    /// <summary>
    /// Gets or sets the contact name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the MAX user identifier of the contact.
    /// </summary>
    [JsonPropertyName("contact_id")]
    public long? ContactId { get; set; }

    /// <summary>
    /// Gets or sets full information about the contact in VCF format.
    /// </summary>
    [JsonPropertyName("vcf_info")]
    public string? VcfInfo { get; set; }

    /// <summary>
    /// Gets or sets the contact phone in VCF format.
    /// </summary>
    [JsonPropertyName("vcf_phone")]
    public string? VcfPhone { get; set; }
}

/// <summary>
/// Payload of a <c>share</c> attachment request.
/// </summary>
public class ShareAttachmentRequestPayload
{
    /// <summary>
    /// Gets or sets the URL attached to the message as a media preview.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the attachment token.
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}
