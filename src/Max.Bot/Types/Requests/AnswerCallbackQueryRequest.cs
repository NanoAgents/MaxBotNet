using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Max.Bot.Types;
using Max.Bot.Types.Enums;

namespace Max.Bot.Types.Requests;

/// <summary>
/// Represents a request to answer a callback query.
/// </summary>
public class AnswerCallbackQueryRequest
{
    /// <summary>
    /// Gets or sets the message body to update the current message.
    /// </summary>
    /// <value>The new message body, or null to keep the existing message.</value>
    [JsonPropertyName("message")]
    public NewMessageBody? Message { get; set; }

    /// <summary>
    /// Gets or sets the notification text to show to the user.
    /// </summary>
    /// <value>The notification text, or null if no notification should be shown.</value>
    [StringLength(200, ErrorMessage = "Notification text must not exceed 200 characters.")]
    [JsonPropertyName("notification")]
    public string? Notification { get; set; }
}

/// <summary>
/// Represents the body of a new message.
/// </summary>
public class NewMessageBody
{
    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    /// <value>The text content, or null if not applicable.</value>
    [StringLength(4000, ErrorMessage = "Text must not exceed 4000 characters.")]
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the attachments for the message.
    /// </summary>
    /// <value>
    /// The attachments array (see <see cref="AttachmentRequest"/> factory methods). If null, existing attachments are not changed.
    /// If empty array, all attachments will be removed.
    /// </value>
    [JsonPropertyName("attachments")]
    public AttachmentRequest[]? Attachments { get; set; }

    /// <summary>
    /// Gets or sets the link to a message (forwarded or reply).
    /// </summary>
    /// <value>The linked message, or null if not applicable.</value>
    [JsonPropertyName("link")]
    public NewMessageLink? Link { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to notify chat participants.
    /// </summary>
    /// <value>True to notify participants (default); otherwise, false.</value>
    [JsonPropertyName("notify")]
    public bool? Notify { get; set; }

    /// <summary>
    /// Gets or sets the text format for the message content.
    /// </summary>
    /// <value>The text format (markdown or html), or null for plain text.</value>
    [JsonPropertyName("format")]
    public TextFormat? Format { get; set; }
}

