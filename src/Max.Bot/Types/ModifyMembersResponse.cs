using System.Text.Json.Serialization;

namespace Max.Bot.Types;

/// <summary>
/// Represents the result of <c>POST /chats/{chatId}/members</c>: success flag plus the users that could not be added.
/// </summary>
public class ModifyMembersResponse : Response
{
    /// <summary>
    /// Gets or sets the identifiers of users that could not be added or removed.
    /// </summary>
    [JsonPropertyName("failed_user_ids")]
    public long[]? FailedUserIds { get; set; }

    /// <summary>
    /// Gets or sets the reasons why users could not be added, grouped by error code.
    /// </summary>
    [JsonPropertyName("failed_user_details")]
    public FailedUserDetails[]? FailedUserDetails { get; set; }
}

/// <summary>
/// Describes why some users could not be added to a chat.
/// </summary>
public class FailedUserDetails
{
    /// <summary>
    /// Gets or sets the error code, for example <c>add.participant.privacy</c> (privacy settings) or
    /// <c>add.participant.not.found</c> (user not found).
    /// </summary>
    [JsonPropertyName("error_code")]
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifiers of users that failed with <see cref="ErrorCode"/>.
    /// </summary>
    [JsonPropertyName("user_ids")]
    public long[] UserIds { get; set; } = Array.Empty<long>();
}
