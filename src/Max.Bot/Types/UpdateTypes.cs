using System.ComponentModel.DataAnnotations;

namespace Max.Bot.Types;

/// <summary>
/// Represents a message_edited update from Max Messenger.
/// </summary>
public class MessageEditedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user locale for this update.
    /// </summary>
    public string? UserLocale { get; set; }

    /// <summary>
    /// Gets or sets the edited message.
    /// </summary>
    public Message Message { get; set; } = null!;
}

/// <summary>
/// Represents a message_removed update from Max Messenger.
/// </summary>
public class MessageRemovedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user locale for this update.
    /// </summary>
    public string? UserLocale { get; set; }

    /// <summary>
    /// Gets or sets the removed message.
    /// </summary>
    public Message? Message { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the removed message (<c>message_id</c>).
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the chat identifier where the message was removed.
    /// </summary>
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who removed the message.
    /// </summary>
    public long? UserId { get; set; }
}

/// <summary>
/// Represents a bot_added update from Max Messenger.
/// Triggered when the bot is added to a chat.
/// </summary>
public class BotAddedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat where the bot was added.
    /// </summary>
    public Chat? Chat { get; set; }

    /// <summary>
    /// Gets or sets the user (bot) that was added.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who invited the bot.
    /// </summary>
    public long? InviterId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the chat the bot was added to.
    /// </summary>
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bot was added to a channel.
    /// </summary>
    public bool? IsChannel { get; set; }
}

/// <summary>
/// Represents a bot_removed update from Max Messenger.
/// Triggered when the bot is removed from a chat.
/// </summary>
public class BotRemovedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat where the bot was removed.
    /// </summary>
    public Chat? Chat { get; set; }

    /// <summary>
    /// Gets or sets the user (bot) that was removed.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the chat the bot was removed from.
    /// </summary>
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bot was removed from a channel.
    /// </summary>
    public bool? IsChannel { get; set; }
}

/// <summary>
/// Represents a bot_started update from Max Messenger.
/// Triggered when a user starts the bot.
/// </summary>
public class BotStartedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat ID where the bot was started.
    /// </summary>
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets the user who started the bot.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    public string? UserLocale { get; set; }

    /// <summary>
    /// Gets or sets the deep-link payload.
    /// </summary>
    public string? Payload { get; set; }
}

/// <summary>
/// Represents a bot_stopped update from Max Messenger.
/// Triggered when a user stops the bot.
/// </summary>
public class BotStoppedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat ID where the bot was stopped.
    /// </summary>
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets the user who stopped the bot.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    public string? UserLocale { get; set; }
}

/// <summary>
/// Represents a user_added update from Max Messenger.
/// Triggered when a user is added to a chat.
/// </summary>
public class UserAddedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat where the user was added.
    /// </summary>
    public Chat? Chat { get; set; }

    /// <summary>
    /// Gets or sets the user that was added.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who invited the user.
    /// </summary>
    public long? InviterId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the chat the user was added to.
    /// </summary>
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user was added to a channel.
    /// </summary>
    public bool? IsChannel { get; set; }
}

/// <summary>
/// Represents a user_removed update from Max Messenger.
/// Triggered when a user is removed from a chat.
/// </summary>
public class UserRemovedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat where the user was removed.
    /// </summary>
    public Chat? Chat { get; set; }

    /// <summary>
    /// Gets or sets the user that was removed.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the chat the user was removed from.
    /// </summary>
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the administrator who removed the user.
    /// </summary>
    public long? AdminId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user was removed from a channel.
    /// </summary>
    public bool? IsChannel { get; set; }
}

/// <summary>
/// Represents a chat_title_changed update from Max Messenger.
/// Triggered when a chat title is changed.
/// </summary>
public class ChatTitleChangedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat with the new title.
    /// </summary>
    public Chat? Chat { get; set; }

    /// <summary>
    /// Gets or sets the user who changed the title.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the chat whose title changed.
    /// </summary>
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets the new chat title.
    /// </summary>
    public string? Title { get; set; }
}

/// <summary>
/// Represents a dialog_muted update from Max Messenger.
/// Triggered when a dialog is muted.
/// </summary>
public class DialogMutedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat ID of the muted dialog.
    /// </summary>
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets whether the dialog is muted.
    /// </summary>
    public bool? IsMuted { get; set; }

    /// <summary>
    /// Gets or sets the time (Unix milliseconds) until which the dialog is muted.
    /// </summary>
    public long? MutedUntil { get; set; }

    /// <summary>
    /// Gets or sets the user who muted the dialog.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    public string? UserLocale { get; set; }
}

/// <summary>
/// Represents a dialog_unmuted update from Max Messenger.
/// Triggered when a dialog is unmuted.
/// </summary>
public class DialogUnmutedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat ID of the unmuted dialog.
    /// </summary>
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets whether the dialog is muted.
    /// </summary>
    public bool? IsMuted { get; set; }

    /// <summary>
    /// Gets or sets the user who unmuted the dialog.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    public string? UserLocale { get; set; }
}

/// <summary>
/// Represents a dialog_cleared update from Max Messenger.
/// Triggered when a dialog history is cleared.
/// </summary>
public class DialogClearedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat ID of the cleared dialog.
    /// </summary>
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets the user who cleared the dialog.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    public string? UserLocale { get; set; }
}

/// <summary>
/// Represents a dialog_removed update from Max Messenger.
/// Triggered when a dialog is removed.
/// </summary>
public class DialogRemovedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the chat ID of the removed dialog.
    /// </summary>
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets the user who removed the dialog.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    public string? UserLocale { get; set; }
}

/// <summary>
/// Represents a message_chat_created update from Max Messenger.
/// Triggered when a chat is created via message.
/// </summary>
public class MessageChatCreatedUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user locale.
    /// </summary>
    public string? UserLocale { get; set; }

    /// <summary>
    /// Gets or sets the message that created the chat.
    /// </summary>
    public Message? Message { get; set; }

    /// <summary>
    /// Gets or sets the created chat.
    /// </summary>
    public Chat? Chat { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the message where the button was clicked.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Gets or sets the payload from the chat button (<c>start_payload</c>).
    /// </summary>
    public string? StartPayload { get; set; }
}







