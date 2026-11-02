using System;
using System.Threading;
using System.Threading.Tasks;
using Max.Bot.Api;
using Max.Bot.Configuration;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Microsoft.Extensions.Logging;

namespace Max.Bot.Polling;

internal static class UpdateHandlerExecutor
{
    public static async Task ExecuteAsync(
        Update update,
        IUpdateHandler handler,
        MaxBotOptions options,
        IMaxBotApi api,
        ILogger? logger,
        IServiceProvider? services,
        CancellationToken cancellationToken)
    {
        using var handlerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (options.Handling.HandlerTimeout > TimeSpan.Zero)
        {
            handlerCts.CancelAfter(options.Handling.HandlerTimeout);
        }

        var context = new UpdateContext(update, api, options, logger, services);

        try
        {
            // * For type-specific updates (Message, CallbackQuery), skip HandleUpdateAsync to avoid double processing
            // This prevents calling both HandleUpdateAsync and HandleMessageAsync/HandleCallbackQueryAsync
            // User should implement either HandleUpdateAsync (for all) or HandleMessageAsync/HandleCallbackQueryAsync (for specific)
            switch (update.Type)
            {
                case UpdateType.MessageCreated:
                case UpdateType.MessageEdited:
                case UpdateType.MessageRemoved:
                case UpdateType.MessageChatCreated:
                    // * Skip HandleUpdateAsync for messages - only call HandleMessageAsync to avoid double processing
                    await handler.HandleMessageAsync(context, handlerCts.Token).ConfigureAwait(false);
                    break;
                case UpdateType.MessageCallback:
                    // * Skip HandleUpdateAsync for callbacks - only call HandleCallbackQueryAsync to avoid double processing
                    await handler.HandleCallbackQueryAsync(context, handlerCts.Token).ConfigureAwait(false);
                    break;
                case UpdateType.Unknown:
                    // * For unknown types, call HandleUpdateAsync first, then HandleUnknownUpdateAsync
                    await handler.HandleUpdateAsync(context, handlerCts.Token).ConfigureAwait(false);
                    await handler.HandleUnknownUpdateAsync(context, handlerCts.Token).ConfigureAwait(false);
                    break;
                default:
                    // * For other types (bot_added, bot_removed, user_added, etc.), call HandleUpdateAsync
                    await handler.HandleUpdateAsync(context, handlerCts.Token).ConfigureAwait(false);
                    break;
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && handlerCts.IsCancellationRequested)
        {
            if (options.Handling.PropagateHandlerExceptions)
            {
                throw;
            }

            logger?.LogWarning(ex, "Update handler timed out after {Timeout}. UpdateId={UpdateId}", options.Handling.HandlerTimeout, update.UpdateId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // * Polling stopped or the webhook request was cancelled: not a handler failure, let the caller see the cancellation.
            throw;
        }
        catch (Exception ex)
        {
            if (options.Handling.PropagateHandlerExceptions)
            {
                throw;
            }

            logger?.LogError(ex, "Update handler threw an exception for update {UpdateId}.", update.UpdateId);
        }
    }
}




