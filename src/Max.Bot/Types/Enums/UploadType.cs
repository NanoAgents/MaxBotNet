using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;

namespace Max.Bot.Types.Enums;

/// <summary>
/// Represents the type of file to upload.
/// </summary>
[JsonConverter(typeof(JsonSnakeCaseEnumConverter<UploadType>))]
public enum UploadType
{
    /// <summary>
    /// Image file.
    /// Serialized as <c>"image"</c>.
    /// </summary>
    Image,

    /// <summary>
    /// Video file.
    /// Serialized as <c>"video"</c>.
    /// </summary>
    Video,

    /// <summary>
    /// Audio file.
    /// Serialized as <c>"audio"</c>.
    /// </summary>
    Audio,

    /// <summary>
    /// Generic file.
    /// Serialized as <c>"file"</c>.
    /// </summary>
    File
}
