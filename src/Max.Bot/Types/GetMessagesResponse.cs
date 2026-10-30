using System.Text.Json.Serialization;

namespace Max.Bot.Types;

/// <summary>
/// Represents the response from the GetMessages endpoint.
/// </summary>
public class GetMessagesResponse
{
    /// <summary>
    /// Gets or sets the list of messages.
    /// </summary>
    /// <value>The list of messages.</value>
    [JsonPropertyName("messages")]
    public Message[] Messages { get; set; } = Array.Empty<Message>();
}
