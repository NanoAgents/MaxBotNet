using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Max.Bot.Types;

/// <summary>
/// Represents the result of uploading file data to an upload URL.
/// </summary>
/// <remarks>
/// MAX API returns different shapes depending on the upload type:
/// <list type="bullet">
/// <item><description><c>image</c>: <c>{"photos": {"&lt;key&gt;": {"token": "..."}}}</c> — <see cref="Photos"/> is filled and <see cref="Token"/> is set to the first photo token.</description></item>
/// <item><description><c>file</c>: <c>{"file_id": ..., "token": "..."}</c>.</description></item>
/// <item><description><c>video</c>/<c>audio</c>: the token is issued by <c>POST /uploads</c> (<see cref="UploadResponse.Token"/>);
/// use <c>IFilesApi.UploadAsync</c> to get it filled automatically.</description></item>
/// </list>
/// </remarks>
public class FileUploadResult
{
    /// <summary>
    /// Gets or sets the uploaded media token that can be used in an attachment payload.
    /// </summary>
    [JsonPropertyName("token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the uploaded file identifier (returned for <c>file</c> uploads).
    /// </summary>
    [JsonPropertyName("file_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? FileId { get; set; }

    /// <summary>
    /// Gets or sets the photo tokens returned after an image upload, keyed by photo identifier.
    /// </summary>
    [JsonPropertyName("photos")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, PhotoSizeToken>? Photos { get; set; }

    /// <summary>
    /// Gets or sets additional properties received in the response.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalProperties { get; set; }

    /// <summary>
    /// Fills <see cref="Token"/> from <see cref="Photos"/> when the response contains only photo tokens.
    /// </summary>
    internal void NormalizeToken()
    {
        if (!string.IsNullOrEmpty(Token) || Photos == null || Photos.Count == 0)
        {
            return;
        }

        Token = Photos.Values.Select(photo => photo?.Token).FirstOrDefault(token => !string.IsNullOrEmpty(token));
    }
}

/// <summary>
/// Represents a token of an uploaded image returned in <see cref="FileUploadResult.Photos"/>.
/// </summary>
public class PhotoSizeToken
{
    /// <summary>
    /// Gets or sets the encoded information of the uploaded image.
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
