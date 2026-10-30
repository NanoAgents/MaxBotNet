using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;

namespace Max.Bot.Types.Enums;

/// <summary>
/// Represents the type of an update from Max Messenger API.
/// Maps to the "update_type" field in the API response.
/// </summary>
[JsonConverter(typeof(UpdateTypeJsonConverter))]
public enum UpdateType
{
    /// <summary>
    /// Unknown update type. Used as fallback for unrecognized types.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// New message created (update_type: "message_created").
    /// </summary>
    MessageCreated = 1,

    /// <summary>
    /// Callback query from inline keyboard button press (update_type: "message_callback").
    /// </summary>
    MessageCallback = 2,

    /// <summary>
    /// Message was edited (update_type: "message_edited").
    /// </summary>
    MessageEdited = 3,

    /// <summary>
    /// Message was removed (update_type: "message_removed").
    /// </summary>
    MessageRemoved = 4,

    /// <summary>
    /// Bot was added to a chat (update_type: "bot_added").
    /// </summary>
    BotAdded = 5,

    /// <summary>
    /// Bot was removed from a chat (update_type: "bot_removed").
    /// </summary>
    BotRemoved = 6,

    /// <summary>
    /// Bot was started by a user (update_type: "bot_started").
    /// </summary>
    BotStarted = 7,

    /// <summary>
    /// Bot was stopped by a user (update_type: "bot_stopped").
    /// </summary>
    BotStopped = 8,

    /// <summary>
    /// Dialog was muted (update_type: "dialog_muted").
    /// </summary>
    DialogMuted = 9,

    /// <summary>
    /// Dialog was unmuted (update_type: "dialog_unmuted").
    /// </summary>
    DialogUnmuted = 10,

    /// <summary>
    /// Dialog was cleared (update_type: "dialog_cleared").
    /// </summary>
    DialogCleared = 11,

    /// <summary>
    /// Dialog was removed (update_type: "dialog_removed").
    /// </summary>
    DialogRemoved = 12,

    /// <summary>
    /// User was added to a chat (update_type: "user_added").
    /// </summary>
    UserAdded = 13,

    /// <summary>
    /// User was removed from a chat (update_type: "user_removed").
    /// </summary>
    UserRemoved = 14,

    /// <summary>
    /// Chat title was changed (update_type: "chat_title_changed").
    /// </summary>
    ChatTitleChanged = 15,

    /// <summary>
    /// Chat was created via message (update_type: "message_chat_created").
    /// </summary>
    MessageChatCreated = 16
}
