using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Max.Bot.Types;

/// <summary>
/// Represents an image in Max Messenger.
/// </summary>
public class Image
{
    /// <summary>
    /// Gets or sets the ID of the image.
    /// </summary>
    [JsonPropertyName("photo_id")]
    public long? PhotoId { get; set; }

    /// <summary>
    /// Gets or sets the URL of the image. Always sent by MAX API.
    /// </summary>
    [JsonPropertyName("url")]
    [JsonConverter(typeof(Converters.NullAsEmptyStringConverter))]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token of the image.
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the tokens received after image upload.
    /// </summary>
    [JsonPropertyName("photos")]
    public Dictionary<string, PhotoSizeToken>? Photos { get; set; }
}
