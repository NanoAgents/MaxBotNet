using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Max.Bot.Types.Enums;

namespace Max.Bot.Types;

/// <summary>
/// Represents a button of a reply keyboard (<see cref="ReplyKeyboardAttachment"/>).
/// </summary>
public class ReplyKeyboardButton
{
    /// <summary>
    /// Gets or sets the type of the button.
    /// </summary>
    [JsonPropertyName("type")]
    public ReplyButtonType Type { get; set; }

    /// <summary>
    /// Gets or sets the visible text of the button.
    /// </summary>
    [Required(ErrorMessage = "Text is required.")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Text must be between 1 and 128 characters.")]
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payload sent with the message when a <see cref="ReplyButtonType.Message"/> button is pressed.
    /// </summary>
    [StringLength(1024, ErrorMessage = "Payload must not exceed 1024 characters.")]
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Gets or sets the intent of a <see cref="ReplyButtonType.Message"/> button. Affects how the client displays it.
    /// </summary>
    [JsonPropertyName("intent")]
    public ButtonIntent? Intent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a <see cref="ReplyButtonType.UserGeoLocation"/> button sends the location
    /// without asking the user for confirmation.
    /// </summary>
    [JsonPropertyName("quick")]
    public bool? Quick { get; set; }

    /// <summary>
    /// Creates a button that sends a message with the given payload.
    /// </summary>
    /// <param name="text">The visible text of the button.</param>
    /// <param name="payload">The payload delivered to the bot as <see cref="DataAttachment.Data"/>.</param>
    /// <param name="intent">The optional intent of the button.</param>
    /// <returns>A new reply keyboard button.</returns>
    public static ReplyKeyboardButton Message(string text, string? payload = null, ButtonIntent? intent = null) =>
        new() { Type = ReplyButtonType.Message, Text = text, Payload = payload, Intent = intent };

    /// <summary>
    /// Creates a button that sends the user's geo location.
    /// </summary>
    /// <param name="text">The visible text of the button.</param>
    /// <param name="quick"><c>true</c> to send the location without confirmation.</param>
    /// <returns>A new reply keyboard button.</returns>
    public static ReplyKeyboardButton GeoLocation(string text, bool? quick = null) =>
        new() { Type = ReplyButtonType.UserGeoLocation, Text = text, Quick = quick };

    /// <summary>
    /// Creates a button that sends the user's contact.
    /// </summary>
    /// <param name="text">The visible text of the button.</param>
    /// <returns>A new reply keyboard button.</returns>
    public static ReplyKeyboardButton Contact(string text) =>
        new() { Type = ReplyButtonType.UserContact, Text = text };
}
