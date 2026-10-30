using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;
using Max.Bot.Types.Enums;

namespace Max.Bot.Types;

/// <summary>
/// Represents the recipient of a message (can be a chat or user).
/// </summary>
public class MessageRecipient
{
    /// <summary>
    /// Gets or sets the chat ID of the recipient.
    /// </summary>
    /// <value>The unique identifier of the chat, or <c>null</c> if MAX did not send it.</value>
    [JsonPropertyName("chat_id")]
    public long? ChatId { get; set; }

    /// <summary>
    /// Gets or sets the type of the chat: <see cref="Enums.ChatType.Dialog"/>, <see cref="Enums.ChatType.Chat"/> or <see cref="Enums.ChatType.Channel"/>.
    /// </summary>
    /// <value>The type of the chat, or <c>null</c> if it is missing or unknown to this library version.</value>
    [JsonPropertyName("chat_type")]
    [JsonConverter(typeof(LenientNullableEnumConverter<ChatType>))]
    public ChatType? ChatType { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the recipient (for dialogs).
    /// </summary>
    /// <value>The unique identifier of the user, or null if not applicable.</value>
    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }
}
