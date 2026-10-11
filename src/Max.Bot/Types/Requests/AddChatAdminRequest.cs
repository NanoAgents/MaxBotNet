using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using Max.Bot.Types.Enums;

namespace Max.Bot.Types.Requests;

/// <summary>
/// Represents a request to assign chat administrators (<c>POST /chats/{chatId}/members/admins</c>).
/// </summary>
public class AddChatAdminRequest
{
    /// <summary>
    /// Gets or sets the administrators to assign together with their permissions.
    /// </summary>
    [JsonPropertyName("admins")]
    public ChatAdmin[] Admins { get; set; } = Array.Empty<ChatAdmin>();

    /// <summary>
    /// Gets or sets the identifier of a single user to make an administrator.
    /// </summary>
    /// <remarks>
    /// Convenience shortcut for a single administrator: setting it replaces <see cref="Admins"/> with one entry
    /// that keeps the permissions of the previous first entry. Prefer <see cref="Admins"/> to set permissions explicitly.
    /// </remarks>
    [JsonIgnore]
    [Range(1, long.MaxValue, ErrorMessage = "User ID must be greater than zero.")]
    public long UserId
    {
        get => Admins.FirstOrDefault()?.UserId ?? 0;
        set
        {
            var existing = Admins.FirstOrDefault();
            Admins = new[]
            {
                new ChatAdmin
                {
                    UserId = value,
                    Permissions = existing?.Permissions ?? Array.Empty<ChatAdminPermission>(),
                    Alias = existing?.Alias
                }
            };
        }
    }
}

/// <summary>
/// Represents a chat administrator with permissions.
/// </summary>
public class ChatAdmin
{
    /// <summary>
    /// Gets or sets the administrator user identifier.
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "User ID must be greater than zero.")]
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    /// <summary>
    /// Gets or sets the administrator permissions.
    /// </summary>
    [JsonPropertyName("permissions")]
    public ChatAdminPermission[] Permissions { get; set; } = Array.Empty<ChatAdminPermission>();

    /// <summary>
    /// Gets or sets the administrator title shown in the chat. Optional.
    /// </summary>
    [JsonPropertyName("alias")]
    public string? Alias { get; set; }
}
