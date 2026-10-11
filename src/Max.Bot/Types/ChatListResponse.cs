using System.Text.Json.Serialization;

namespace Max.Bot.Types;

/// <summary>
/// Represents a page of chats returned by <c>GET /chats</c>.
/// </summary>
public class ChatListResponse
{
    /// <summary>
    /// Gets or sets the chats on this page.
    /// </summary>
    [JsonPropertyName("chats")]
    public Chat[] Chats { get; set; } = Array.Empty<Chat>();

    /// <summary>
    /// Gets or sets the marker of the next page, or null when there are no more chats.
    /// </summary>
    [JsonPropertyName("marker")]
    public long? Marker { get; set; }
}
