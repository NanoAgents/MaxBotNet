using System.Text.Json;
using System.Text.Json.Serialization;

namespace Max.Bot.Types;

/// <summary>
/// Represents the URLs to download or play a video (<c>urls</c> of <c>GET /videos/{token}</c>).
/// Each rendition is <c>null</c> if it is not available.
/// </summary>
public class VideoUrls
{
    /// <summary>
    /// Gets or sets the MP4 URL in 1080p (<c>mp4_1080</c>).
    /// </summary>
    [JsonPropertyName("mp4_1080")]
    public string? Mp4P1080 { get; set; }

    /// <summary>
    /// Gets or sets the MP4 URL in 720p (<c>mp4_720</c>).
    /// </summary>
    [JsonPropertyName("mp4_720")]
    public string? Mp4P720 { get; set; }

    /// <summary>
    /// Gets or sets the MP4 URL in 480p (<c>mp4_480</c>).
    /// </summary>
    [JsonPropertyName("mp4_480")]
    public string? Mp4P480 { get; set; }

    /// <summary>
    /// Gets or sets the MP4 URL in 360p (<c>mp4_360</c>).
    /// </summary>
    [JsonPropertyName("mp4_360")]
    public string? Mp4P360 { get; set; }

    /// <summary>
    /// Gets or sets the MP4 URL in 240p (<c>mp4_240</c>).
    /// </summary>
    [JsonPropertyName("mp4_240")]
    public string? Mp4P240 { get; set; }

    /// <summary>
    /// Gets or sets the MP4 URL in 144p (<c>mp4_144</c>).
    /// </summary>
    [JsonPropertyName("mp4_144")]
    public string? Mp4P144 { get; set; }

    /// <summary>
    /// Gets or sets the live streaming (HLS) URL (<c>hls</c>).
    /// </summary>
    [JsonPropertyName("hls")]
    public string? Hls { get; set; }

    /// <summary>
    /// Gets or sets renditions unknown to this library version, keyed by their API name.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalUrls { get; set; }

    /// <summary>
    /// Gets the URL of the best available MP4 rendition, or <c>null</c> if there is none.
    /// </summary>
    [JsonIgnore]
    public string? BestMp4 => Mp4P1080 ?? Mp4P720 ?? Mp4P480 ?? Mp4P360 ?? Mp4P240 ?? Mp4P144;
}
