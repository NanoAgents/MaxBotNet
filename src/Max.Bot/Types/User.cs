using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Max.Bot.Types;

/// <summary>
/// Represents a Max Messenger user.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    /// <value>The unique identifier of the user.</value>
    [Range(1, long.MaxValue, ErrorMessage = "User ID must be greater than zero.")]
    [JsonPropertyName("user_id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the username of the user.
    /// </summary>
    /// <value>The username of the user, or null if not available.</value>
    [StringLength(64, MinimumLength = 1, ErrorMessage = "Username must be between 1 and 64 characters.")]
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the first name of the user.
    /// </summary>
    /// <value>The first name of the user. Always sent by MAX API.</value>
    [StringLength(64, ErrorMessage = "First name must not exceed 64 characters.")]
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last name of the user.
    /// </summary>
    /// <value>The last name of the user, or null if not available.</value>
    [StringLength(64, ErrorMessage = "Last name must not exceed 64 characters.")]
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is a bot.
    /// </summary>
    /// <value>True if the user is a bot; otherwise, false.</value>
    [JsonPropertyName("is_bot")]
    public bool IsBot { get; set; }

    /// <summary>
    /// Gets or sets the last activity time of the user (Unix timestamp in milliseconds).
    /// </summary>
    /// <value>The timestamp of the user's last activity.</value>
    [Range(0, long.MaxValue, ErrorMessage = "Last activity time cannot be negative.")]
    [JsonPropertyName("last_activity_time")]
    public long LastActivityTime { get; set; }

    /// <summary>
    /// Gets or sets the deprecated display name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the bot or profile description.
    /// </summary>
    [StringLength(16000, ErrorMessage = "Description must not exceed 16000 characters.")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the small avatar URL.
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the full-size avatar URL.
    /// </summary>
    [JsonPropertyName("full_avatar_url")]
    public string? FullAvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the bot commands.
    /// </summary>
    [MaxLength(32, ErrorMessage = "Commands must not exceed 32 items.")]
    [JsonPropertyName("commands")]
    public BotCommand[]? Commands { get; set; }
}

