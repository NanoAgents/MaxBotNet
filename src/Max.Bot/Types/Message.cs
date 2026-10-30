// 📁 Message.cs - Core message DTOs for Max API payloads
// 🎯 Core function: Models Message and nested link/body/stat objects.
// 🔗 Key dependencies: System.Text.Json, data annotations, custom converters.
// 💡 Usage: Used in updates, send/edit responses, and message operations.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;
using Max.Bot.Types.Enums;

namespace Max.Bot.Types;

/// <summary>
/// Represents a message in Max Messenger chat.
/// Maps to the Message object in the official Max API documentation.
/// </summary>
public class Message
{
    /// <summary>
    /// Gets or sets the user who sent the message.
    /// Can be <c>null</c> if the message was posted on behalf of a channel.
    /// Maps to "sender" field from API.
    /// </summary>
    [JsonPropertyName("sender")]
    public User? Sender { get; set; }

    /// <summary>
    /// Gets or sets the recipient of the message.
    /// Can be a user or a chat. Always present in MAX API responses.
    /// </summary>
    [JsonPropertyName("recipient")]
    [JsonConverter(typeof(Converters.NullAsNewConverter<MessageRecipient>))]
    public MessageRecipient Recipient { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the message was created (Unix timestamp in milliseconds).
    /// Maps to "timestamp" field from API.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonPropertyName("timestamp")]
    [JsonConverter(typeof(Converters.NullAsDefaultConverter<long>))]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the linked message (forwarded or reply message).
    /// Maps to "link" field from API.
    /// </summary>
    [JsonPropertyName("link")]
    public LinkedMessage? Link { get; set; }

    /// <summary>
    /// Gets or sets the body of the message (text and attachments).
    /// Can be null if message contains only forwarded message.
    /// Maps to "body" field from API.
    /// </summary>
    [JsonPropertyName("body")]
    public MessageBody? Body { get; set; }

    /// <summary>
    /// Gets or sets the statistics of the message.
    /// Available only for channel messages returned by GET /messages.
    /// Maps to "stat" field from API.
    /// </summary>
    [JsonPropertyName("stat")]
    public MessageStat? Stat { get; set; }

    /// <summary>
    /// Gets or sets the public URL of the message.
    /// Can be null for dialogs or non-public chats.
    /// Maps to "url" field from API.
    /// </summary>
    [StringLength(512, ErrorMessage = "Url must not exceed 512 characters.")]
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// This is a convenience property that accesses Body.Text.
    /// Not part of the official API - use Body.Text for direct access.
    /// </summary>
    [JsonIgnore]
    public string? Text
    {
        get => Body?.Text;
        set
        {
            Body ??= new MessageBody();
            Body.Text = value;
        }
    }

    /// <summary>
    /// Gets the message ID from Body.Mid.
    /// </summary>
    [JsonIgnore]
    public string? Mid => Body?.Mid;
}

/// <summary>
/// Represents a linked (forwarded or reply) message.
/// Maps to LinkedMessage in the official Max API documentation.
/// </summary>
public class LinkedMessage
{
    /// <summary>
    /// Gets or sets the type of link: <see cref="MessageLinkType.Forward"/> or <see cref="MessageLinkType.Reply"/>.
    /// </summary>
    /// <value>The link type, or <c>null</c> if MAX sent a value unknown to this library version.</value>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(LenientNullableEnumConverter<MessageLinkType>))]
    public MessageLinkType? Type { get; set; }

    /// <summary>
    /// Gets or sets the user who sent the original message.
    /// Can be <c>null</c> if the message was posted on behalf of a channel.
    /// </summary>
    [JsonPropertyName("sender")]
    public User? Sender { get; set; }

    /// <summary>
    /// Gets or sets the chat where the message was originally posted. Present for forwarded messages only.
    /// </summary>
    [JsonPropertyName("chat_id")]
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets the message body (contains mid, seq, text, attachments).
    /// Note: In API this is called "message" but contains only MessageBody fields, not full Message.
    /// </summary>
    [JsonPropertyName("message")]
    [JsonConverter(typeof(Converters.NullAsNewConverter<MessageBody>))]
    public MessageBody Message { get; set; } = new();

    /// <summary>
    /// Gets the text content of the linked message.
    /// </summary>
    [JsonIgnore]
    public string? Text => Message?.Text;

    /// <summary>
    /// Gets the message ID of the linked message.
    /// </summary>
    [JsonIgnore]
    public string Mid => Message?.Mid ?? string.Empty;

    /// <summary>
    /// Gets the sequence number of the linked message.
    /// </summary>
    [JsonIgnore]
    public long Seq => Message?.Seq ?? 0;
}

/// <summary>
/// Represents the body of a message containing text and attachments.
/// Maps to MessageBody in the official Max API documentation.
/// </summary>
public class MessageBody
{
    /// <summary>
    /// Gets or sets the message ID.
    /// </summary>
    [JsonPropertyName("mid")]
    [JsonConverter(typeof(Converters.NullAsEmptyStringConverter))]
    public string Mid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sequence number of the message.
    /// Used for ordering messages.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Sequence number cannot be negative.")]
    [JsonPropertyName("seq")]
    [JsonConverter(typeof(Converters.NullAsDefaultConverter<long>))]
    public long Seq { get; set; }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the attachments of the message.
    /// </summary>
    [JsonPropertyName("attachments")]
    public Attachment[]? Attachments { get; set; }

    /// <summary>
    /// Gets or sets the text markup (bold, links, mentions, ...) applied to <see cref="Text"/>.
    /// </summary>
    [JsonPropertyName("markup")]
    public MarkupElement[]? Markup { get; set; }
}

/// <summary>
/// Represents statistics of a message.
/// Maps to MessageStat in the official Max API documentation.
/// </summary>
public class MessageStat
{
    /// <summary>
    /// Gets or sets the number of times the message was read.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "ReadCount cannot be negative.")]
    [JsonPropertyName("read_count")]
    public int? ReadCount { get; set; }

    /// <summary>
    /// Gets or sets the number of message views (returned by MAX API for channel messages).
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Views cannot be negative.")]
    [JsonPropertyName("views")]
    [JsonConverter(typeof(Converters.NullAsDefaultConverter<int>))]
    public int Views { get; set; }
}

/// <summary>
/// Represents a formatting element of a message text.
/// </summary>
public class MarkupElement
{
    /// <summary>
    /// Gets or sets the markup type (strong, emphasized, monospaced, link, strikethrough, underline, user_mention, heading, highlighted, quote).
    /// </summary>
    /// <value>The markup type, or <c>null</c> if MAX sent a value unknown to this library version.</value>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(LenientNullableEnumConverter<MarkupType>))]
    public MarkupType? Type { get; set; }

    /// <summary>
    /// Gets or sets the start position (in UTF-16 code units) of the element in the text.
    /// </summary>
    [JsonPropertyName("from")]
    public int From { get; set; }

    /// <summary>
    /// Gets or sets the length of the element in UTF-16 code units.
    /// </summary>
    [JsonPropertyName("length")]
    public int Length { get; set; }

    /// <summary>
    /// Gets or sets the URL for <c>link</c> markup.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the mentioned user identifier for <c>user_mention</c> markup.
    /// </summary>
    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }

    /// <summary>
    /// Gets or sets the mentioned user link for <c>user_mention</c> markup.
    /// </summary>
    [JsonPropertyName("user_link")]
    public string? UserLink { get; set; }
}
