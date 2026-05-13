using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Max.Bot.Types;

/// <summary>
/// Represents a video in Max Messenger.
/// </summary>
public class Video
{
    /// <summary>
    /// Gets or sets the media token returned by /videos/{token}.
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the video.
    /// </summary>
    /// <value>The unique identifier of the video.</value>
    [Range(1, long.MaxValue, ErrorMessage = "Video ID must be greater than zero.")]
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the file identifier of the video.
    /// </summary>
    /// <value>The file identifier of the video.</value>
    [Required(ErrorMessage = "File ID is required.")]
    [StringLength(256, MinimumLength = 1, ErrorMessage = "File ID must be between 1 and 256 characters.")]
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the width of the video in pixels.
    /// </summary>
    /// <value>The width of the video in pixels, or null if not available.</value>
    [Range(1, int.MaxValue, ErrorMessage = "Width must be greater than zero if provided.")]
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the video in pixels.
    /// </summary>
    /// <value>The height of the video in pixels, or null if not available.</value>
    [Range(1, int.MaxValue, ErrorMessage = "Height must be greater than zero if provided.")]
    [JsonPropertyName("height")]
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the duration of the video in seconds.
    /// </summary>
    /// <value>The duration of the video in seconds, or null if not available.</value>
    [Range(1, int.MaxValue, ErrorMessage = "Duration must be greater than zero if provided.")]
    [JsonPropertyName("duration")]
    public int? Duration { get; set; }

    /// <summary>
    /// Gets or sets the size of the video file in bytes.
    /// </summary>
    /// <value>The size of the video file in bytes, or null if not available.</value>
    [Range(1, long.MaxValue, ErrorMessage = "File size must be greater than zero if provided.")]
    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the video.
    /// </summary>
    /// <value>The MIME type of the video (e.g., "video/mp4"), or null if not available.</value>
    [StringLength(64, ErrorMessage = "MIME type must not exceed 64 characters.")]
    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }

    /// <summary>
    /// Gets or sets the URL of the video.
    /// </summary>
    /// <value>The URL of the video, or null if not available.</value>
    [Url(ErrorMessage = "URL must be a valid URL if provided.")]
    [StringLength(2048, ErrorMessage = "URL must not exceed 2048 characters.")]
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets quality-specific video URLs keyed by rendition name (for example, mp4_720).
    /// </summary>
    [JsonPropertyName("urls")]
    public Dictionary<string, string>? Urls { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail for this video.
    /// </summary>
    [JsonPropertyName("thumbnail")]
    public VideoThumbnail? Thumbnail { get; set; }
}

/// <summary>
/// Represents a preview image for a video.
/// </summary>
public class VideoThumbnail
{
    /// <summary>
    /// Gets or sets the thumbnail URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

