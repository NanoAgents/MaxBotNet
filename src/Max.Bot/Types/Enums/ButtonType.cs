using System.Text.Json.Serialization;

namespace Max.Bot.Types.Enums;

/// <summary>
/// Represents the type of an inline keyboard button.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ButtonType
{
    /// <summary>
    /// Callback button - server sends a callback event when pressed.
    /// </summary>
    Callback,

    /// <summary>
    /// Link button - opens a URL in a new tab.
    /// </summary>
    Link,

    /// <summary>
    /// Message button - sends a text message to the bot.
    /// </summary>
    Message,

    /// <summary>
    /// Request contact button - requests user's contact and phone number.
    /// </summary>
    RequestContact,

    /// <summary>
    /// Request geo location button - requests user's location.
    /// </summary>
    RequestGeoLocation,

    /// <summary>
    /// Open app button - opens a mini application (<c>open_app</c>) set by <see cref="InlineKeyboardButton.WebApp"/>.
    /// Without <see cref="InlineKeyboardButton.WebApp"/> the button is sent as a <c>link</c> for backward compatibility.
    /// </summary>
    OpenApp,

    /// <summary>
    /// Clipboard button - copies <see cref="InlineKeyboardButton.Payload"/> to the user's clipboard.
    /// </summary>
    Clipboard
}








