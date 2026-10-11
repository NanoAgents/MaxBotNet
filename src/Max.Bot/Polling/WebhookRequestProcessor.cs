using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Max.Bot.Configuration;
using Max.Bot.Networking;
using Max.Bot.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Max.Bot.Polling;

/// <summary>
/// Shared request pipeline for <see cref="WebhookController"/> and <c>MapMaxWebhook</c>:
/// size limit and secret are checked before the body is read and parsed.
/// </summary>
internal static class WebhookRequestProcessor
{
    public static async Task<int> ProcessAsync(
        HttpContext context,
        MaxBotOptions options,
        IUpdatePipeline pipeline,
        IUpdateHandler handler,
        ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(handler);

        if (!WebhookRequestValidator.ValidatePayloadSize(context.Request, options, logger))
        {
            return StatusCodes.Status413PayloadTooLarge;
        }

        if (!WebhookRequestValidator.ValidateSignature(context.Request, options, logger))
        {
            return StatusCodes.Status401Unauthorized;
        }

        var body = await ReadBodyAsync(context, WebhookRequestValidator.GetMaxBodyBytes(options)).ConfigureAwait(false);
        if (body == null)
        {
            logger?.LogWarning("Webhook payload rejected: body exceeds the configured size limit.");
            return StatusCodes.Status413PayloadTooLarge;
        }

        Update? update;
        try
        {
            update = body.Length == 0 ? null : JsonSerializer.Deserialize<Update>(body, MaxJsonSerializer.Options);
        }
        catch (JsonException ex)
        {
            logger?.LogWarning(ex, "Webhook payload rejected: invalid JSON.");
            update = null;
        }

        if (update == null)
        {
            return StatusCodes.Status400BadRequest;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        var handlerTimeout = options.Webhook.HandlerTimeout;
        if (handlerTimeout > TimeSpan.Zero && handlerTimeout != Timeout.InfiniteTimeSpan)
        {
            timeoutCts.CancelAfter(handlerTimeout);
        }

        try
        {
            await pipeline.ProcessWebhookAsync(update, handler, context.RequestServices, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !context.RequestAborted.IsCancellationRequested)
        {
            // * Answer MAX anyway: a non-2xx status would make MAX deliver the same update again.
            logger?.LogWarning(ex, "Webhook update handling exceeded Webhook.HandlerTimeout ({Timeout}).", handlerTimeout);
        }

        return StatusCodes.Status200OK;
    }

    private static async Task<byte[]?> ReadBodyAsync(HttpContext context, long maxBytes)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await context.Request.Body.ReadAsync(chunk, context.RequestAborted).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
