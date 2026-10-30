using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;

namespace Max.Bot.Types.Enums;

/// <summary>
/// Represents the type of a text markup element (<see cref="MarkupElement.Type"/>).
/// </summary>
[JsonConverter(typeof(JsonSnakeCaseEnumConverter<MarkupType>))]
public enum MarkupType
{
    /// <summary>
    /// Bold text. Serialized as <c>"strong"</c>.
    /// </summary>
    Strong,

    /// <summary>
    /// Italic text. Serialized as <c>"emphasized"</c>.
    /// </summary>
    Emphasized,

    /// <summary>
    /// Monospaced text or code block. Serialized as <c>"monospaced"</c>.
    /// </summary>
    Monospaced,

    /// <summary>
    /// Link (<see cref="MarkupElement.Url"/>). Serialized as <c>"link"</c>.
    /// </summary>
    Link,

    /// <summary>
    /// Strikethrough text. Serialized as <c>"strikethrough"</c>.
    /// </summary>
    Strikethrough,

    /// <summary>
    /// Underlined text. Serialized as <c>"underline"</c>.
    /// </summary>
    Underline,

    /// <summary>
    /// User mention (<see cref="MarkupElement.UserId"/> or <see cref="MarkupElement.UserLink"/>). Serialized as <c>"user_mention"</c>.
    /// </summary>
    UserMention,

    /// <summary>
    /// Heading. Serialized as <c>"heading"</c>.
    /// </summary>
    Heading,

    /// <summary>
    /// Highlighted text. Serialized as <c>"highlighted"</c>.
    /// </summary>
    Highlighted,

    /// <summary>
    /// Quote block. Serialized as <c>"quote"</c>.
    /// </summary>
    Quote
}
