using Max.Bot.Types.Enums;

namespace Max.Bot.Types.Helpers;

/// <summary>
/// Helper class for converting between <see cref="UpdateType"/> enum and string representations.
/// </summary>
public static class UpdateTypeHelper
{
    /// <summary>
    /// Converts an <see cref="UpdateType"/> enum value to its string representation.
    /// </summary>
    /// <param name="updateType">The update type to convert.</param>
    /// <returns>The snake_case string representation (e.g., "message_created").</returns>
    public static string ToStringValue(UpdateType updateType)
    {
        return updateType switch
        {
            UpdateType.MessageCreated => "message_created",
            UpdateType.MessageCallback => "message_callback",
            UpdateType.MessageEdited => "message_edited",
            UpdateType.MessageRemoved => "message_removed",
            UpdateType.BotAdded => "bot_added",
            UpdateType.BotRemoved => "bot_removed",
            UpdateType.BotStarted => "bot_started",
            UpdateType.BotStopped => "bot_stopped",
            UpdateType.DialogMuted => "dialog_muted",
            UpdateType.DialogUnmuted => "dialog_unmuted",
            UpdateType.DialogCleared => "dialog_cleared",
            UpdateType.DialogRemoved => "dialog_removed",
            UpdateType.UserAdded => "user_added",
            UpdateType.UserRemoved => "user_removed",
            UpdateType.ChatTitleChanged => "chat_title_changed",
            UpdateType.MessageChatCreated => "message_chat_created",
            UpdateType.Unknown => "unknown",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Converts a string representation to <see cref="UpdateType"/> enum.
    /// </summary>
    /// <param name="updateTypeString">The snake_case string (e.g., "message_created"); PascalCase names (e.g., "MessageCreated") are accepted too.</param>
    /// <returns>The corresponding <see cref="UpdateType"/> enum value, or <see cref="UpdateType.Unknown"/> if not recognized.</returns>
    public static UpdateType FromString(string? updateTypeString)
    {
        if (string.IsNullOrEmpty(updateTypeString))
            return UpdateType.Unknown;

        return updateTypeString switch
        {
            "message_created" => UpdateType.MessageCreated,
            "message_callback" => UpdateType.MessageCallback,
            "message_edited" => UpdateType.MessageEdited,
            "message_removed" => UpdateType.MessageRemoved,
            "bot_added" => UpdateType.BotAdded,
            "bot_removed" => UpdateType.BotRemoved,
            "bot_started" => UpdateType.BotStarted,
            "bot_stopped" => UpdateType.BotStopped,
            "dialog_muted" => UpdateType.DialogMuted,
            "dialog_unmuted" => UpdateType.DialogUnmuted,
            "dialog_cleared" => UpdateType.DialogCleared,
            "dialog_removed" => UpdateType.DialogRemoved,
            "user_added" => UpdateType.UserAdded,
            "user_removed" => UpdateType.UserRemoved,
            "chat_title_changed" => UpdateType.ChatTitleChanged,
            "message_chat_created" => UpdateType.MessageChatCreated,
            _ => Converters.LenientEnumParser.TryParse<UpdateType>(updateTypeString, out var parsed) ? parsed : UpdateType.Unknown
        };
    }

    /// <summary>
    /// Converts a collection of <see cref="UpdateType"/> enums to their string representations.
    /// </summary>
    /// <param name="updateTypes">The collection of update types to convert.</param>
    /// <returns>A collection of snake_case string representations.</returns>
    public static IEnumerable<string> ToStringValues(IEnumerable<UpdateType> updateTypes)
    {
        if (updateTypes == null)
            return Enumerable.Empty<string>();

        return updateTypes.Select(ToStringValue);
    }

    /// <summary>
    /// Converts a collection of string representations to <see cref="UpdateType"/> enums.
    /// </summary>
    /// <param name="updateTypeStrings">The collection of snake_case strings to convert.</param>
    /// <returns>A collection of <see cref="UpdateType"/> enum values.</returns>
    public static IEnumerable<UpdateType> FromStrings(IEnumerable<string?> updateTypeStrings)
    {
        if (updateTypeStrings == null)
            return Enumerable.Empty<UpdateType>();

        return updateTypeStrings.Select(FromString);
    }
}
