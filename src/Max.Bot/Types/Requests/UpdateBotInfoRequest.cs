using System.Text.Json.Serialization;

namespace Max.Bot.Types.Requests;

/// <summary>
/// Represents a request to update bot commands via PATCH /me/commands.
/// </summary>
public class UpdateBotInfoRequest
{
    /// <summary>
    /// Gets or sets the list of bot commands to set.
    /// </summary>
    /// <value>The array of bot commands, or null to leave unchanged.</value>
    [JsonPropertyName("commands")]
    public BotCommand[]? Commands { get; set; }
}

