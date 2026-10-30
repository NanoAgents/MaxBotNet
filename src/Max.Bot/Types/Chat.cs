// 📁 Chat.cs - Chat DTO aligned with Max API
// 🎯 Core function: Models chat identity and metadata from API.
// 🔗 Key dependencies: ChatType/ChatStatus enums, JSON attributes.
// 💡 Usage: Appears in chat endpoints and some update types.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Max.Bot.Types.Enums;

namespace Max.Bot.Types;

/// <summary>
/// Represents a Max Messenger chat.
/// Maps to the Chat object in the official Max API documentation.
/// </summary>
public class Chat
{
    /// <summary>
    /// Gets or sets the unique identifier of the chat.
    /// Maps to "chat_id" field from API.
    /// Note: Can be negative for channels, 0 for some dialogs, or positive for groups/chats.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    /// <summary>
    /// Gets or sets the type of the chat.
    /// Possible values: "chat" (group chat), "dialog" (private chat).
    /// </summary>
    [JsonPropertyName("type")]
    public ChatType Type { get; set; }

    /// <summary>
    /// Gets or sets the status of the chat.
    /// Possible values: "active", "removed", "left", "closed".
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(Converters.LenientNullableEnumConverter<ChatStatus>))]
    public ChatStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the title of the chat.
    /// Can be null for dialogs.
    /// </summary>
    [StringLength(256, ErrorMessage = "Title must not exceed 256 characters.")]
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the icon of the chat.
    /// </summary>
    [JsonPropertyName("icon")]
    public Image? Icon { get; set; }

    /// <summary>
    /// Gets or sets the time of the last event in the chat (Unix timestamp in milliseconds).
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "LastEventTime cannot be negative.")]
    [JsonPropertyName("last_event_time")]
    [JsonConverter(typeof(Converters.NullAsDefaultConverter<long>))]
    public long LastEventTime { get; set; }

    /// <summary>
    /// Gets or sets the number of participants in the chat.
    /// Always 2 for dialogs.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "ParticipantsCount cannot be negative.")]
    [JsonPropertyName("participants_count")]
    [JsonConverter(typeof(Converters.NullAsDefaultConverter<int>))]
    public int ParticipantsCount { get; set; }

    /// <summary>
    /// Gets or sets the ID of the chat owner.
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "OwnerId must be greater than zero.")]
    [JsonPropertyName("owner_id")]
    public long? OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the participants of the chat: user ID → time of the last activity (Unix timestamp in milliseconds).
    /// Visible to chat admins only; can be null when requesting a list of chats.
    /// </summary>
    [JsonPropertyName("participants")]
    public Dictionary<long, long>? Participants { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the chat is public.
    /// Always false for dialogs.
    /// </summary>
    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    /// <summary>
    /// Gets or sets the link to the chat.
    /// </summary>
    [StringLength(512, ErrorMessage = "Link must not exceed 512 characters.")]
    [JsonPropertyName("link")]
    public string? Link { get; set; }

    /// <summary>
    /// Gets or sets the description of the chat.
    /// </summary>
    [StringLength(512, ErrorMessage = "Description must not exceed 512 characters.")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the user data in the dialog (only for chats of type "dialog").
    /// </summary>
    [JsonPropertyName("dialog_with_user")]
    public User? DialogWithUser { get; set; }

    /// <summary>
    /// Gets or sets the ID of the message containing the button that initiated the chat.
    /// </summary>
    [StringLength(256, ErrorMessage = "ChatMessageId must not exceed 256 characters.")]
    [JsonPropertyName("chat_message_id")]
    public string? ChatMessageId { get; set; }

    /// <summary>
    /// Gets or sets the pinned message in the chat.
    /// Returned only when requesting a specific chat.
    /// </summary>
    [JsonPropertyName("pinned_message")]
    public Message? PinnedMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of messages in the chat. Only for group chats and channels.
    /// </summary>
    [JsonPropertyName("messages_count")]
    public long? MessagesCount { get; set; }
}
