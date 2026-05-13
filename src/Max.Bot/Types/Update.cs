using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;
using Max.Bot.Types.Enums;
using Max.Bot.Types.Helpers;

namespace Max.Bot.Types;

/// <summary>
/// Represents an update from Max Messenger API.
/// Different update types contain different fields.
/// </summary>
[JsonConverter(typeof(UpdateJsonConverter))]
public class Update
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the raw update_type string from API.
    /// </summary>
    /// <remarks>
    /// Possible values: message_created, message_callback, message_edited, message_removed,
    /// bot_added, bot_removed, bot_started, bot_stopped, dialog_muted, dialog_unmuted,
    /// dialog_cleared, dialog_removed, user_added, user_removed, chat_title_changed, message_chat_created.
    /// </remarks>
    [JsonPropertyName("update_type")]
    public string? UpdateTypeRaw { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user locale for this update.
    /// Available only in dialogs.
    /// </summary>
    /// <value>The locale code in IETF BCP 47 format (e.g., "ru", "en").</value>
    [JsonPropertyName("user_locale")]
    public string? UserLocale { get; set; }

    /// <summary>
    /// Gets the type of the update as enum.
    /// </summary>
    [JsonIgnore]
    public UpdateType Type => ParseUpdateType(UpdateTypeRaw);

    /// <summary>
    /// Gets or sets the message in this update.
    /// Present in: message_created, message_edited, message_removed, message_chat_created.
    /// </summary>
    [JsonPropertyName("message")]
    public Message? Message { get; set; }

    /// <summary>
    /// Gets or sets the callback query in this update.
    /// Present in: message_callback.
    /// Maps to "callback" field in API.
    /// </summary>
    [JsonIgnore]
    public CallbackQuery? Callback { get; set; }

    /// <summary>
    /// Gets or sets the chat in this update.
    /// Present in: bot_added, bot_removed, user_added, user_removed, chat_title_changed.
    /// </summary>
    [JsonIgnore]
    public Chat? Chat { get; set; }

    /// <summary>
    /// Gets or sets the user who triggered the update.
    /// Present in: bot_added, bot_removed, bot_started, bot_stopped, user_added, user_removed.
    /// </summary>
    [JsonIgnore]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who invited the bot/user.
    /// Present in: bot_added, user_added.
    /// </summary>
    [JsonIgnore]
    public long? InviterId { get; set; }

    /// <summary>
    /// Gets or sets the chat ID.
    /// Present in: dialog_muted, dialog_unmuted, dialog_cleared, dialog_removed.
    /// </summary>
    [JsonIgnore]
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets whether the chat is muted.
    /// Present in: dialog_muted, dialog_unmuted.
    /// </summary>
    [JsonIgnore]
    public bool? IsMuted { get; set; }

    /// <summary>
    /// Gets or sets the bot start payload.
    /// </summary>
    [JsonIgnore]
    public string? Payload { get; set; }

    #region Typed Update Wrappers

    /// <summary>
    /// Gets the typed message update wrapper for message_created events.
    /// </summary>
    [JsonIgnore]
    public MessageUpdate? MessageUpdate
    {
        get
        {
            if (Message == null || Type != UpdateType.MessageCreated)
                return null;

            return new MessageUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                UserLocale = UserLocale,
                Message = Message
            };
        }
    }

    /// <summary>
    /// Gets the typed callback query update wrapper for message_callback events.
    /// </summary>
    [JsonIgnore]
    public CallbackQueryUpdate? CallbackQueryUpdate
    {
        get
        {
            if (Callback == null || Type != UpdateType.MessageCallback)
                return null;

            return new CallbackQueryUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                UserLocale = UserLocale,
                CallbackQuery = Callback,
                Message = Message
            };
        }
    }

    /// <summary>
    /// Gets the typed message edited update wrapper.
    /// </summary>
    [JsonIgnore]
    public MessageEditedUpdate? MessageEditedUpdate
    {
        get
        {
            if (Message == null || Type != UpdateType.MessageEdited)
                return null;

            return new MessageEditedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                UserLocale = UserLocale,
                Message = Message
            };
        }
    }

    /// <summary>
    /// Gets the typed message removed update wrapper.
    /// </summary>
    [JsonIgnore]
    public MessageRemovedUpdate? MessageRemovedUpdate
    {
        get
        {
            if (Message == null || Type != UpdateType.MessageRemoved)
                return null;

            return new MessageRemovedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                UserLocale = UserLocale,
                Message = Message
            };
        }
    }

    /// <summary>
    /// Gets the typed bot added update wrapper.
    /// </summary>
    [JsonIgnore]
    public BotAddedUpdate? BotAddedUpdate
    {
        get
        {
            if (Type != UpdateType.BotAdded)
                return null;

            return new BotAddedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                Chat = Chat,
                User = User,
                InviterId = InviterId
            };
        }
    }

    /// <summary>
    /// Gets the typed bot removed update wrapper.
    /// </summary>
    [JsonIgnore]
    public BotRemovedUpdate? BotRemovedUpdate
    {
        get
        {
            if (Type != UpdateType.BotRemoved)
                return null;

            return new BotRemovedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                Chat = Chat,
                User = User
            };
        }
    }

    /// <summary>
    /// Gets the typed bot started update wrapper.
    /// </summary>
    [JsonIgnore]
    public BotStartedUpdate? BotStartedUpdate
    {
        get
        {
            if (Type != UpdateType.BotStarted)
                return null;

            return new BotStartedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                ChatId = ChatId,
                User = User,
                UserLocale = UserLocale,
                Payload = Payload
            };
        }
    }

    /// <summary>
    /// Gets the typed bot stopped update wrapper.
    /// </summary>
    [JsonIgnore]
    public BotStoppedUpdate? BotStoppedUpdate
    {
        get
        {
            if (Type != UpdateType.BotStopped)
                return null;

            return new BotStoppedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                ChatId = ChatId,
                User = User
            };
        }
    }

    /// <summary>
    /// Gets the typed user added update wrapper.
    /// </summary>
    [JsonIgnore]
    public UserAddedUpdate? UserAddedUpdate
    {
        get
        {
            if (Type != UpdateType.UserAdded)
                return null;

            return new UserAddedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                Chat = Chat,
                User = User,
                InviterId = InviterId
            };
        }
    }

    /// <summary>
    /// Gets the typed user removed update wrapper.
    /// </summary>
    [JsonIgnore]
    public UserRemovedUpdate? UserRemovedUpdate
    {
        get
        {
            if (Type != UpdateType.UserRemoved)
                return null;

            return new UserRemovedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                Chat = Chat,
                User = User
            };
        }
    }

    /// <summary>
    /// Gets the typed chat title changed update wrapper.
    /// </summary>
    [JsonIgnore]
    public ChatTitleChangedUpdate? ChatTitleChangedUpdate
    {
        get
        {
            if (Type != UpdateType.ChatTitleChanged)
                return null;

            return new ChatTitleChangedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                Chat = Chat,
                User = User
            };
        }
    }

    /// <summary>
    /// Gets the typed dialog muted update wrapper.
    /// </summary>
    [JsonIgnore]
    public DialogMutedUpdate? DialogMutedUpdate
    {
        get
        {
            if (Type != UpdateType.DialogMuted)
                return null;

            return new DialogMutedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                ChatId = ChatId,
                IsMuted = IsMuted
            };
        }
    }

    /// <summary>
    /// Gets the typed dialog unmuted update wrapper.
    /// </summary>
    [JsonIgnore]
    public DialogUnmutedUpdate? DialogUnmutedUpdate
    {
        get
        {
            if (Type != UpdateType.DialogUnmuted)
                return null;

            return new DialogUnmutedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                ChatId = ChatId,
                IsMuted = IsMuted
            };
        }
    }

    /// <summary>
    /// Gets the typed dialog cleared update wrapper.
    /// </summary>
    [JsonIgnore]
    public DialogClearedUpdate? DialogClearedUpdate
    {
        get
        {
            if (Type != UpdateType.DialogCleared)
                return null;

            return new DialogClearedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                ChatId = ChatId
            };
        }
    }

    /// <summary>
    /// Gets the typed dialog removed update wrapper.
    /// </summary>
    [JsonIgnore]
    public DialogRemovedUpdate? DialogRemovedUpdate
    {
        get
        {
            if (Type != UpdateType.DialogRemoved)
                return null;

            return new DialogRemovedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                ChatId = ChatId
            };
        }
    }

    /// <summary>
    /// Gets the typed message chat created update wrapper.
    /// </summary>
    [JsonIgnore]
    public MessageChatCreatedUpdate? MessageChatCreatedUpdate
    {
        get
        {
            if (Message == null || Type != UpdateType.MessageChatCreated)
                return null;

            return new MessageChatCreatedUpdate
            {
                UpdateId = UpdateId,
                Timestamp = Timestamp,
                UserLocale = UserLocale,
                Message = Message
            };
        }
    }

    #endregion

    /// <summary>
    /// Parses the raw update_type string to UpdateType enum.
    /// </summary>
    private static UpdateType ParseUpdateType(string? updateTypeRaw)
    {
        return UpdateTypeHelper.FromString(updateTypeRaw);
    }
}
