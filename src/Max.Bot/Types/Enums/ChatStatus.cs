using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;

namespace Max.Bot.Types.Enums;

/// <summary>
/// Represents the status of a chat.
/// </summary>
[JsonConverter(typeof(JsonSnakeCaseEnumConverter<ChatStatus>))]
public enum ChatStatus
{
    /// <summary>
    /// Bot is an active participant in the chat.
    /// </summary>
    Active,

    /// <summary>
    /// Bot was removed from the chat.
    /// </summary>
    Removed,

    /// <summary>
    /// Bot left the chat.
    /// </summary>
    Left,

    /// <summary>
    /// Chat was closed.
    /// </summary>
    Closed,

    /// <summary>
    /// Chat was suspended.
    /// </summary>
    Suspended
}
