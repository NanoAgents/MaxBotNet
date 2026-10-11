using System;
using System.Collections.Generic;
using System.Linq;
using Max.Bot.Configuration;
using Max.Bot.Types;
using Max.Bot.Types.Enums;

namespace Max.Bot.Polling;

internal static class UpdateFilterUtilities
{
    public static HashSet<UpdateType>? BuildTypeFilter(MaxBotOptions options)
    {
        var candidates = options.Handling.AllowedUpdateTypes;
        if (candidates is { Count: > 0 })
        {
            return new HashSet<UpdateType>(candidates);
        }

        candidates = options.Polling.AllowedUpdateTypes;
        if (candidates is { Count: > 0 })
        {
            return new HashSet<UpdateType>(candidates);
        }

        return null;
    }

    public static HashSet<string>? BuildAllowedUsernames(MaxBotOptions options)
    {
        if (options.Handling.AllowedUsernames is { Count: > 0 })
        {
            return new HashSet<string>(
                options.Handling.AllowedUsernames.Where(static name => !string.IsNullOrWhiteSpace(name)),
                StringComparer.OrdinalIgnoreCase);
        }

        return null;
    }

    public static bool ShouldDispatch(Update update, HashSet<UpdateType>? typeFilter, HashSet<string>? allowedUsernames)
    {
        if (typeFilter != null && !typeFilter.Contains(update.Type))
        {
            return false;
        }

        if (allowedUsernames != null && allowedUsernames.Count > 0)
        {
            var username = ExtractUsername(update);
            if (string.IsNullOrWhiteSpace(username) || !allowedUsernames.Contains(username))
            {
                return false;
            }
        }

        return true;
    }

    public static string? ExtractUsername(Update update)
    {
        // * In message_callback the message is the bot's own message with the keyboard: the presser is callback.user.
        return update.Callback?.User?.Username ??
               update.Message?.Sender?.Username ??
               update.User?.Username;
    }
}






