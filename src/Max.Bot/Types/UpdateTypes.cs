using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Max.Bot.Types;

/// <summary>
/// Represents a message_edited update from Max Messenger.
/// </summary>
public class MessageEditedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user locale for this update.
    /// </summary>
    [JsonPropertyName("user_locale")]
    public string? UserLocale { get; set; }

    /// <summary>
    /// Gets or sets the edited message.
    /// </summary>
    [JsonPropertyName("message")]
    public Message Message { get; set; } = null!;
}

/// <summary>
/// Represents a message_removed update from Max Messenger.
/// </summary>
public class MessageRemovedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user locale for this update.
    /// </summary>
    [JsonPropertyName("user_locale")]
    public string? UserLocale { get; set; }

    /// <summary>
    /// Gets or sets the removed message.
    /// </summary>
    [JsonPropertyName("message")]
    public Message? Message { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the removed message (<c>message_id</c>).
    /// </summary>
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the chat identifier where the message was removed.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who removed the message.
    /// </summary>
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }
}

/// <summary>
/// Represents a bot_added update from Max Messenger.
/// Triggered when the bot is added to a chat.
/// </summary>
public class BotAddedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat where the bot was added.
    /// </summary>
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }

    /// <summary>
    /// Gets or sets the user who added the bot to the chat.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = new();

    /// <summary>
    /// Gets or sets the ID of the user who added the bot. MAX sends it as <see cref="User"/>, so it equals <c>User.Id</c>.
    /// </summary>
    [JsonPropertyName("inviter_id")]
    public long? InviterId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the chat the bot was added to.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bot was added to a channel.
    /// </summary>
    [JsonPropertyName("is_channel")]
    public bool IsChannel { get; set; }
}

/// <summary>
/// Represents a bot_removed update from Max Messenger.
/// Triggered when the bot is removed from a chat.
/// </summary>
public class BotRemovedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat where the bot was removed.
    /// </summary>
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }

    /// <summary>
    /// Gets or sets the user who removed the bot from the chat.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = new();

    /// <summary>
    /// Gets or sets the identifier of the chat the bot was removed from.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bot was removed from a channel.
    /// </summary>
    [JsonPropertyName("is_channel")]
    public bool IsChannel { get; set; }
}

/// <summary>
/// Represents a bot_started update from Max Messenger.
/// Triggered when a user starts the bot.
/// </summary>
public class BotStartedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat ID where the bot was started.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets the user who started the bot.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = new();

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    [JsonPropertyName("user_locale")]
    public string? UserLocale { get; set; }

    /// <summary>
    /// Gets or sets the deep-link payload.
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}

/// <summary>
/// Represents a bot_stopped update from Max Messenger.
/// Triggered when a user stops the bot.
/// </summary>
public class BotStoppedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat ID where the bot was stopped.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets the user who stopped the bot.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = new();

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    [JsonPropertyName("user_locale")]
    public string? UserLocale { get; set; }
}

/// <summary>
/// Represents a user_added update from Max Messenger.
/// Triggered when a user is added to a chat.
/// </summary>
public class UserAddedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat where the user was added.
    /// </summary>
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }

    /// <summary>
    /// Gets or sets the user that was added.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = new();

    /// <summary>
    /// Gets or sets the ID of the user who invited the user.
    /// </summary>
    [JsonPropertyName("inviter_id")]
    public long? InviterId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the chat the user was added to.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user was added to a channel.
    /// </summary>
    [JsonPropertyName("is_channel")]
    public bool IsChannel { get; set; }
}

/// <summary>
/// Represents a user_removed update from Max Messenger.
/// Triggered when a user is removed from a chat.
/// </summary>
public class UserRemovedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat where the user was removed.
    /// </summary>
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }

    /// <summary>
    /// Gets or sets the user that was removed.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = new();

    /// <summary>
    /// Gets or sets the identifier of the chat the user was removed from.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the administrator who removed the user.
    /// </summary>
    [JsonPropertyName("admin_id")]
    public long? AdminId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user was removed from a channel.
    /// </summary>
    [JsonPropertyName("is_channel")]
    public bool IsChannel { get; set; }
}

/// <summary>
/// Represents a chat_title_changed update from Max Messenger.
/// Triggered when a chat title is changed.
/// </summary>
public class ChatTitleChangedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat with the new title.
    /// </summary>
    [JsonPropertyName("chat")]
    public Chat? Chat { get; set; }

    /// <summary>
    /// Gets or sets the user who changed the title.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = new();

    /// <summary>
    /// Gets or sets the identifier of the chat whose title changed.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets the new chat title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Represents a dialog_muted update from Max Messenger.
/// Triggered when a dialog is muted.
/// </summary>
public class DialogMutedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat ID of the muted dialog.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets whether the dialog is muted.
    /// </summary>
    [JsonPropertyName("is_muted")]
    public bool? IsMuted { get; set; }

    /// <summary>
    /// Gets or sets the time (Unix milliseconds) until which the dialog is muted.
    /// </summary>
    [JsonPropertyName("muted_until")]
    public long MutedUntil { get; set; }

    /// <summary>
    /// Gets or sets the user who muted the dialog.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = new();

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    [JsonPropertyName("user_locale")]
    public string? UserLocale { get; set; }
}

/// <summary>
/// Represents a dialog_unmuted update from Max Messenger.
/// Triggered when a dialog is unmuted.
/// </summary>
public class DialogUnmutedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat ID of the unmuted dialog.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets whether the dialog is muted.
    /// </summary>
    [JsonPropertyName("is_muted")]
    public bool? IsMuted { get; set; }

    /// <summary>
    /// Gets or sets the user who unmuted the dialog.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = new();

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    [JsonPropertyName("user_locale")]
    public string? UserLocale { get; set; }
}

/// <summary>
/// Represents a dialog_cleared update from Max Messenger.
/// Triggered when a dialog history is cleared.
/// </summary>
public class DialogClearedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat ID of the cleared dialog.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets the user who cleared the dialog.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = new();

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    [JsonPropertyName("user_locale")]
    public string? UserLocale { get; set; }
}

/// <summary>
/// Represents a dialog_removed update from Max Messenger.
/// Triggered when a dialog is removed.
/// </summary>
public class DialogRemovedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat ID of the removed dialog.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets the user who removed the dialog.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = new();

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    [JsonPropertyName("user_locale")]
    public string? UserLocale { get; set; }
}

/// <summary>
/// Represents a message_chat_created update from Max Messenger.
/// Triggered when a chat is created via message.
/// </summary>
public class MessageChatCreatedUpdate
{
    /// <summary>
    /// Gets or sets the identifier of the update. MAX Bot API does not send <c>update_id</c>, so it is <c>0</c> for API updates;
    /// use <see cref="GetUpdatesResponse.Marker"/> for long polling.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    [JsonPropertyName("user_locale")]
    public string? UserLocale { get; set; }

    /// <summary>
    /// Gets or sets the message that created the chat.
    /// </summary>
    [JsonPropertyName("message")]
    public Message? Message { get; set; }

    /// <summary>
    /// Gets or sets the created chat.
    /// </summary>
    [JsonPropertyName("chat")]
    public Chat Chat { get; set; } = new();

    /// <summary>
    /// Gets or sets the identifier of the created chat.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the message where the button was clicked.
    /// </summary>
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payload from the chat button (<c>start_payload</c>).
    /// </summary>
    [JsonPropertyName("start_payload")]
    public string? StartPayload { get; set; }
}







