using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;

namespace Max.Bot.Types.Enums;

/// <summary>
/// Represents the text format for message content.
/// </summary>
[JsonConverter(typeof(JsonSnakeCaseEnumConverter<TextFormat>))]
public enum TextFormat
{
    /// <summary>
    /// Markdown formatting.
    /// </summary>
    Markdown,

    /// <summary>
    /// HTML formatting.
    /// </summary>
    Html
}
