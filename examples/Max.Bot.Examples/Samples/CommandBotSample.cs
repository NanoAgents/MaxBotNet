using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Max.Bot.Polling;
using Max.Bot.Types;

namespace Max.Bot.Examples.Samples;

/// <summary>
/// Demonstrates a minimal command router handling /start, /help, and /stats.
/// </summary>
public sealed class CommandBotSample : IBotSample
{
    private readonly IReadOnlyDictionary<string, Func<UpdateContext, CancellationToken, Task>> _handlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBotSample"/> class.
    /// </summary>
    public CommandBotSample()
    {
        _handlers = new Dictionary<string, Func<UpdateContext, CancellationToken, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            ["/start"] = HandleStartAsync,
            ["/help"] = HandleHelpAsync,
            ["/stats"] = HandleStatsAsync
        };
    }

    /// <inheritdoc />
    public string Name => "commands";

    /// <inheritdoc />
    public string Description => "Processes /start, /help, /stats commands.";

    /// <inheritdoc />
    public Task RunAsync(SampleExecutionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var handler = new DelegatingUpdateHandler(
            onMessage: async (updateContext, ct) =>
            {
                var message = updateContext.Update.Message;
                var chatId = SampleUtilities.GetChatId(message);
                var text = SampleUtilities.GetNormalizedText(message);

                if (!chatId.HasValue || string.IsNullOrWhiteSpace(text) || text[0] != '/')
                {
                    return;
                }

                var command = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                if (_handlers.TryGetValue(command, out var handlerDelegate))
                {
                    await handlerDelegate(updateContext, ct).ConfigureAwait(false);
                    context.Output.WriteLine($"Command '{command}' processed for chat {chatId.Value}.");
                }
                else
                {
                    await updateContext.Api.Messages.SendMessageAsync(
                        chatId.Value,
                        $"Unknown command '{command}'. Try /help.",
                        ct).ConfigureAwait(false);
                }
            });

        return SampleUtilities.RunPollingLoopAsync(context, handler, cancellationToken);
    }

    private static async Task HandleStartAsync(UpdateContext context, CancellationToken cancellationToken)
    {
        var chatId = SampleUtilities.GetChatId(context.Update.Message);
        if (!chatId.HasValue)
        {
            return;
        }

        await context.Api.Messages.SendMessageAsync(
            chatId.Value,
            "👋 Welcome! Use /help to discover available commands.",
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task HandleHelpAsync(UpdateContext context, CancellationToken cancellationToken)
    {
        var chatId = SampleUtilities.GetChatId(context.Update.Message);
        if (!chatId.HasValue)
        {
            return;
        }

        var helpText = string.Join(Environment.NewLine, new[]
        {
            "Available commands:",
            "/start - initializes the session.",
            "/help - shows this help message.",
            "/stats - displays basic chat diagnostics."
        });

        await context.Api.Messages.SendMessageAsync(chatId.Value, helpText, cancellationToken).ConfigureAwait(false);
    }

    private static async Task HandleStatsAsync(UpdateContext context, CancellationToken cancellationToken)
    {
        var message = context.Update.Message;
        var chatId = SampleUtilities.GetChatId(message);
        if (!chatId.HasValue)
        {
            return;
        }

        var chat = await context.Api.Chats.GetChatAsync(chatId.Value, cancellationToken).ConfigureAwait(false);
        var response = $"Chat '{chat.Title ?? chatId.Value.ToString()}' ({chat.Type})";
        await context.Api.Messages.SendMessageAsync(chatId.Value, response, cancellationToken).ConfigureAwait(false);
    }
}





