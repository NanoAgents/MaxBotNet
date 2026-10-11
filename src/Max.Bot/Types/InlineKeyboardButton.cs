using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;
using Max.Bot.Types.Enums;

namespace Max.Bot.Types;

/// <summary>
/// Represents a button in an inline keyboard.
/// </summary>
[JsonConverter(typeof(InlineKeyboardButtonJsonConverter))]
public class InlineKeyboardButton
{
    /// <summary>
    /// Gets or sets the type of the button.
    /// </summary>
    /// <value>The button type (callback, link, message, etc.).</value>
    [Required(ErrorMessage = "Button type is required.")]
    [JsonPropertyName("type")]
    public ButtonType Type { get; set; }

    /// <summary>
    /// Gets or sets the text displayed on the button.
    /// </summary>
    /// <value>The text displayed on the button.</value>
    [Required(ErrorMessage = "Text is required.")]
    [StringLength(64, MinimumLength = 1, ErrorMessage = "Text must be between 1 and 64 characters.")]
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payload data for callback or message buttons.
    /// </summary>
    /// <value>The payload data, or null if not applicable.</value>
    [StringLength(64, ErrorMessage = "Payload must not exceed 64 characters.")]
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Gets or sets the URL to open when the button is pressed (for link buttons).
    /// </summary>
    /// <value>The URL, or null if not applicable.</value>
    [Url(ErrorMessage = "URL must be a valid URL if provided.")]
    [StringLength(2048, ErrorMessage = "URL must not exceed 2048 characters.")]
    [JsonPropertyName("url")]
    public string? Url
    {
        get => _url;
        set
        {
            _url = value;
            if (!string.IsNullOrEmpty(value) && Type == default(ButtonType))
            {
                Type = ButtonType.Link;
            }
        }
    }

    private string? _url;

    /// <summary>
    /// Gets or sets the intent/style of the button.
    /// Only applicable for callback buttons. Affects how the button is displayed by the client.
    /// </summary>
    /// <value>The button intent (default, positive, or negative), or null to use default.</value>
    [JsonPropertyName("intent")]
    public ButtonIntent? Intent { get; set; }

    /// <summary>
    /// Gets or sets the mini application to open (bot username or link) for <see cref="ButtonType.OpenApp"/> buttons.
    /// </summary>
    [JsonPropertyName("web_app")]
    public string? WebApp { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the bot whose mini application is opened by an <see cref="ButtonType.OpenApp"/> button.
    /// </summary>
    [JsonPropertyName("contact_id")]
    public long? ContactId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a <see cref="ButtonType.RequestGeoLocation"/> button sends the location
    /// without asking the user for confirmation.
    /// </summary>
    [JsonPropertyName("quick")]
    public bool? Quick { get; set; }

    /// <summary>
    /// Gets or sets the callback data sent when the button is pressed.
    /// This property is for backward compatibility and automatically sets Type to Callback and Payload.
    /// </summary>
    /// <value>The callback data, or null if not available.</value>
    [JsonIgnore]
    public string? CallbackData
    {
        get => Type == ButtonType.Callback ? Payload : null;
        set
        {
            if (value != null)
            {
                Type = ButtonType.Callback;
                Payload = value;
            }
        }
    }
}

