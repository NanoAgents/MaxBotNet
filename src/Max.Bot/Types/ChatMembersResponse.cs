using System.Text.Json.Serialization;

namespace Max.Bot.Types;

/// <summary>
/// Represents the response from chat members endpoints.
/// </summary>
public class ChatMembersResponse
{
    /// <summary>
    /// Gets or sets the chat members.
    /// </summary>
    [JsonPropertyName("members")]
    public ChatMember[] Members { get; set; } = Array.Empty<ChatMember>();

    /// <summary>
    /// Gets or sets the marker for the next page.
    /// </summary>
    [JsonPropertyName("marker")]
    public long? Marker { get; set; }
}
