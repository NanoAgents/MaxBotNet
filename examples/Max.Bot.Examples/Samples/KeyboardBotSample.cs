using System;
using System.Threading;
using System.Threading.Tasks;
using Max.Bot.Polling;
using Max.Bot.Types;
using Max.Bot.Types.Requests;

namespace Max.Bot.Examples.Samples;

/// <summary>
/// Sample showcasing inline keyboards and callback query processing.
/// </summary>
public sealed class KeyboardBotSample : IBotSample
{
    /// <inheritdoc />
    public string Name => "keyboard";

    /// <inheritdoc />
    public string Description => "Sends inline keyboards and responds to button presses.";

    /// <inheritdoc />
    public Task RunAsync(SampleExecutionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var handler = new DelegatingUpdateHandler(
            onMessage: (updateContext, ct) => HandleMessageAsync(context, updateContext, ct),
            onCallback: (updateContext, ct) => HandleCallbackAsync(context, updateContext, ct));

        return SampleUtilities.RunPollingLoopAsync(context, handler, cancellationToken);
    }

    private static async Task HandleMessageAsync(SampleExecutionContext sampleContext, UpdateContext context, CancellationToken cancellationToken)
    {
        var message = context.Update.Message;
        var chatId = SampleUtilities.GetChatId(message);
        var text = SampleUtilities.GetNormalizedText(message);

        if (!chatId.HasValue || !string.Equals(text, "/buttons", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var keyboard = new InlineKeyboard(new[]
        {
            new[]
            {
                new InlineKeyboardButton { Text = "👍 Approve", CallbackData = "vote:approve" },
                new InlineKeyboardButton { Text = "👎 Reject", CallbackData = "vote:reject" }
            },
            new[]
            {
                new InlineKeyboardButton { Text = "📚 Docs", Url = "https://dev.max.ru/docs-api" }
            }
        });

        await context.Api.Messages.SendMessageAsync(
            chatId.Value,
            "Choose an option:",
            keyboard,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        sampleContext.Output.WriteLine("Inline keyboard sent.");
    }

    private static async Task HandleCallbackAsync(SampleExecutionContext sampleContext, UpdateContext context, CancellationToken cancellationToken)
    {
        var callback = context.Update.Callback;
        if (callback == null)
        {
            return;
        }

        var responseText = callback.Payload switch
        {
            "vote:approve" => "✅ Approved",
            "vote:reject" => "❌ Rejected",
            _ => "ℹ️ Received."
        };

        var request = new AnswerCallbackQueryRequest
        {
            Notification = responseText
        };

        await context.Api.Messages.AnswerCallbackQueryAsync(callback.CallbackId ?? string.Empty, request, cancellationToken).ConfigureAwait(false);
        sampleContext.Output.WriteLine($"Callback '{callback.Payload}' processed.");
    }
}





