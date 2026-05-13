using System.ComponentModel.DataAnnotations;

namespace Max.Bot.Types;

/// <summary>
/// Represents a callback query update from Max Messenger.
/// </summary>
public class CallbackQueryUpdate
{
    /// <summary>
    /// Gets or sets the unique identifier of the update.
    /// </summary>
    /// <value>The unique identifier of the update.</value>
    [Range(0, long.MaxValue, ErrorMessage = "Update ID cannot be negative.")]
    public long UpdateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update (Unix timestamp in milliseconds).
    /// </summary>
    /// <value>The timestamp when the update was created.</value>
    [Range(0, long.MaxValue, ErrorMessage = "Timestamp cannot be negative.")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user locale for this update.
    /// </summary>
    /// <value>The locale code (e.g., "ru", "en").</value>
    public string? UserLocale { get; set; }

    /// <summary>
    /// Gets or sets the callback query in this update.
    /// </summary>
    /// <value>The callback query in this update.</value>
    public CallbackQuery CallbackQuery { get; set; } = null!;

    /// <summary>
    /// Gets or sets the message that contains the callback keyboard.
    /// </summary>
    /// <remarks>
    /// MAX webhook delivers message payload at update level for <c>message_callback</c>,
    /// so consumers should read message from this property.
    /// </remarks>
    public Message? Message { get; set; }
}











