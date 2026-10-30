using System.Text.Json.Serialization;

namespace Max.Bot.Types;

/// <summary>
/// Represents the response from the upload endpoint.
/// </summary>
public class UploadResponse
{
    /// <summary>
    /// Gets or sets the URL for uploading the file.
    /// </summary>
    /// <value>The upload URL.</value>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token for video or audio uploads.
    /// </summary>
    /// <value>The upload token, or null if not available.</value>
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}






