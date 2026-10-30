using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;

namespace Max.Bot.Types.Enums;

/// <summary>
/// Specifies the type of message link.
/// </summary>
[JsonConverter(typeof(JsonSnakeCaseEnumConverter<MessageLinkType>))]
public enum MessageLinkType
{
    /// <summary>
    /// Forward message link type.
    /// Serialized as <c>"forward"</c>.
    /// </summary>
    Forward,

    /// <summary>
    /// Reply message link type.
    /// Serialized as <c>"reply"</c>.
    /// </summary>
    Reply
}
