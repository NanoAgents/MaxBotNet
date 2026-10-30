// 📁 ChatMember.cs - Chat member DTO aligned with real MAX API response
// 🎯 Core function: Models a chat member returned by GET /chats/{chatId}/members and /chats/{chatId}/members/admins
// 🔗 Key dependencies: ChatAdminPermission enum, JSON attributes.

using System.Text.Json.Serialization;
using Max.Bot.Types.Enums;

namespace Max.Bot.Types;

/// <summary>
/// Represents a member of a chat in the Max Messenger API.
/// This type is returned by <c>GET /chats/{chatId}/members</c> and <c>GET /chats/{chatId}/members/admins</c>.
/// It extends the basic <see cref="User"/> model with chat-specific fields such as admin status,
/// join time, avatar URLs, and permissions.
/// </summary>
public class ChatMember
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    [JsonPropertyName("user_id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the user's visible name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the user's unique public username.
    /// Can be null if the user doesn't have one or isn't accessible.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is a bot.
    /// </summary>
    [JsonPropertyName("is_bot")]
    public bool IsBot { get; set; }

    /// <summary>
    /// Gets or sets the user's last activity time (Unix timestamp in milliseconds).
    /// </summary>
    [JsonPropertyName("last_activity_time")]
    public long LastActivityTime { get; set; }

    /// <summary>
    /// Gets or sets the user's description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the user's last access time in this chat.
    /// </summary>
    [JsonPropertyName("last_access_time")]
    public long LastAccessTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is the owner of the chat.
    /// </summary>
    [JsonPropertyName("is_owner")]
    public bool IsOwner { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is an admin of the chat.
    /// </summary>
    [JsonPropertyName("is_admin")]
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Gets or sets the time when the user joined the chat (Unix timestamp in seconds).
    /// </summary>
    [JsonPropertyName("join_time")]
    public long JoinTime { get; set; }

    /// <summary>
    /// Gets or sets the URL of the user's avatar.
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL of the user's avatar in a larger size.
    /// </summary>
    [JsonPropertyName("full_avatar_url")]
    public string? FullAvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the admin permissions for this member.
    /// Only populated if the member is an admin; otherwise null.
    /// </summary>
    [JsonPropertyName("permissions")]
    [JsonConverter(typeof(Converters.LenientEnumArrayConverter<ChatAdminPermission>))]
    public ChatAdminPermission[]? Permissions { get; set; }

    /// <summary>
    /// Gets or sets the alias shown by clients.
    /// </summary>
    [JsonPropertyName("alias")]
    public string? Alias { get; set; }
}
