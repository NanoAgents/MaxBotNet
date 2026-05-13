namespace Max.Bot.Types.Enums;

/// <summary>
/// Represents chat admin permissions returned by the MAX API.
/// Serialized as snake_case by <see cref="Max.Bot.Networking.MaxJsonSerializer"/>
/// (global <c>JsonStringEnumConverter</c> with <c>SnakeCaseLower</c> policy).
/// </summary>
public enum ChatAdminPermission
{
    /// <summary>
    /// Permission to view stats.
    /// Serializes as "view_stats".
    /// </summary>
    ViewStats,

    /// <summary>
    /// Permission to read all messages in the chat.
    /// Serializes as "read_all_messages".
    /// </summary>
    ReadAllMessages,

    /// <summary>
    /// Permission to add or remove members.
    /// Serializes as "add_remove_members".
    /// </summary>
    AddRemoveMembers,

    /// <summary>
    /// Permission to add admins.
    /// Serializes as "add_admins".
    /// </summary>
    AddAdmins,

    /// <summary>
    /// Permission to change chat info (title, icon, etc.).
    /// Serializes as "change_chat_info".
    /// </summary>
    ChangeChatInfo,

    /// <summary>
    /// Permission to pin messages.
    /// Serializes as "pin_message".
    /// </summary>
    PinMessage,

    /// <summary>
    /// Permission to write messages (for restricted chats).
    /// Serializes as "write".
    /// </summary>
    Write,

    /// <summary>
    /// Permission to make calls.
    /// Serializes as "can_call".
    /// </summary>
    CanCall,

    /// <summary>
    /// Permission to edit the chat link.
    /// Serializes as "edit_link".
    /// </summary>
    EditLink,

    /// <summary>
    /// Permission to edit messages (short form in MAX API).
    /// Serializes as "edit".
    /// </summary>
    Edit,

    /// <summary>
    /// Permission to edit or delete posted messages.
    /// Serializes as "post_edit_delete_message".
    /// </summary>
    PostEditDeleteMessage,

    /// <summary>
    /// Permission to edit messages.
    /// Serializes as "edit_message".
    /// </summary>
    EditMessage,

    /// <summary>
    /// Permission to delete messages.
    /// Serializes as "delete_message".
    /// </summary>
    DeleteMessage,

    /// <summary>
    /// Permission to delete messages (short form in MAX API).
    /// Serializes as "delete".
    /// </summary>
    Delete,
}
