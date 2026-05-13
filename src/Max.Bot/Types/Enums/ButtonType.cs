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
    /// Open app button - opens a mini application.
    /// </summary>
    [Obsolete("MAX API currently accepts URL buttons as link. Use ButtonType.Link instead.")]
    OpenApp
}








