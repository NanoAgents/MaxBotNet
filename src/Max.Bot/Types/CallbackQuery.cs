using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;

namespace Max.Bot.Types;

/// <summary>
/// Represents a callback query from an inline button press.
/// </summary>
[JsonConverter(typeof(CallbackQueryJsonConverter))]
public class CallbackQuery
{
    /// <summary>
    /// Gets or sets the callback ID (current keyboard ID).
    /// This property maps to "callback_id" field from API.
    /// </summary>
    /// <value>The callback ID, or null if not available.</value>
    [StringLength(512, ErrorMessage = "Callback ID must not exceed 512 characters.")]
    [JsonIgnore]
    public string? CallbackId { get; set; }

    /// <summary>
    /// Gets or sets the user who pressed the button.
    /// This property maps to "user" field from API.
    /// </summary>
    /// <value>The user who pressed the button.</value>
    [Required(ErrorMessage = "User is required.")]
    [JsonIgnore]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the message with the inline button that was pressed.
    /// </summary>
    /// <value>The message with the inline button, or null if not available.</value>
    [Obsolete("CallbackQuery.Message is not populated by current MAX API webhook contract. Use CallbackQueryUpdate.Message from Update.CallbackQueryUpdate instead.")]
    [JsonIgnore]
    public Message? Message { get; set; }

    /// <summary>
    /// Gets or sets the payload data associated with the callback button.
    /// This property maps to "payload" field from API.
    /// </summary>
    /// <value>The callback payload, or null if not available.</value>
    [StringLength(64, ErrorMessage = "Payload must not exceed 64 characters.")]
    [JsonIgnore]
    public string? Payload { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the user pressed the button (Unix timestamp in milliseconds).
    /// </summary>
    /// <value>The timestamp when the button was pressed, or null if not available.</value>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    [JsonIgnore]
    public long? Timestamp { get; set; }
}

