using System.ComponentModel.DataAnnotations;

namespace Max.Bot.Types;

/// <summary>
/// Represents a message update from Max Messenger.
/// </summary>
public class MessageUpdate
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
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user locale for this update.
    /// </summary>
    /// <value>The locale code (e.g., "ru", "en").</value>
    public string? UserLocale { get; set; }

    /// <summary>
    /// Gets or sets the message in this update.
    /// </summary>
    /// <value>The message in this update.</value>
    public Message Message { get; set; } = null!;
}











