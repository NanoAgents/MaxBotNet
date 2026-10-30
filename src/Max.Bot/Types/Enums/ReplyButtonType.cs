using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;

namespace Max.Bot.Types.Enums;

/// <summary>
/// Represents the type of a reply keyboard button (<see cref="ReplyKeyboardButton.Type"/>).
/// </summary>
[JsonConverter(typeof(JsonSnakeCaseEnumConverter<ReplyButtonType>))]
public enum ReplyButtonType
{
    /// <summary>
    /// Sends a message on behalf of the user; the button payload arrives as <see cref="DataAttachment"/>.
    /// Serialized as <c>"message"</c>.
    /// </summary>
    Message,

    /// <summary>
    /// Sends the user's geo location. Serialized as <c>"user_geo_location"</c>.
    /// </summary>
    UserGeoLocation,

    /// <summary>
    /// Sends the user's contact. Serialized as <c>"user_contact"</c>.
    /// </summary>
    UserContact
}
