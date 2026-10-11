using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Max.Bot.Configuration;
using Max.Bot.Networking;
using Max.Bot.Polling;
using Max.Bot.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods that map the MAX webhook endpoint explicitly.
/// </summary>
public static class MaxWebhookEndpointRouteBuilderExtensions
{
    /// <summary>
    /// The default webhook route.
    /// </summary>
    public const string DefaultPattern = "/api/max/webhook";

    /// <summary>
    /// Maps a <c>POST</c> endpoint that receives MAX webhook updates and dispatches them to the registered <see cref="IUpdateHandler"/>.
    /// </summary>
    /// <remarks>
    /// Requires the following services in the DI container:
    /// <see cref="IUpdatePipeline"/> (or <see cref="Max.Bot.MaxClient"/>), <see cref="IUpdateHandler"/> and
    /// <see cref="MaxBotOptions"/> (or <c>IOptions&lt;MaxBotOptions&gt;</c>).
    /// When <see cref="MaxWebhookOptions.SecretToken"/> is set, requests without a matching
    /// <see cref="MaxWebhookOptions.SignatureHeaderName"/> header (<c>X-Max-Bot-Api-Secret</c> by default) are rejected with 401.
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern. Defaults to <see cref="DefaultPattern"/>.</param>
    /// <returns>A builder that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapMaxWebhook(this IEndpointRouteBuilder endpoints, string pattern = DefaultPattern)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.MapPost(pattern, HandleWebhookAsync);
    }

    private static async Task HandleWebhookAsync(HttpContext context)
    {
        var services = context.RequestServices;
        var options = services.GetService<MaxBotOptions>()
            ?? services.GetService<IOptions<MaxBotOptions>>()?.Value
            ?? throw new InvalidOperationException("MaxBotOptions is not registered in the service container.");
        var pipeline = services.GetService<IUpdatePipeline>()
            ?? services.GetService<Max.Bot.MaxClient>()
            ?? throw new InvalidOperationException("IUpdatePipeline (for example MaxClient) is not registered in the service container.");
        var handler = services.GetService<IUpdateHandler>()
            ?? throw new InvalidOperationException("IUpdateHandler is not registered in the service container.");
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Max.Bot.Webhook");

        if (!WebhookRequestValidator.ValidatePayloadSize(context.Request, options, logger))
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        if (!WebhookRequestValidator.ValidateSignature(context.Request, options, logger))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var body = await ReadBodyAsync(context, WebhookRequestValidator.GetMaxBodyBytes(options)).ConfigureAwait(false);
        if (body == null)
        {
            logger?.LogWarning("Webhook payload rejected: body exceeds the configured size limit.");
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
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
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await pipeline.ProcessWebhookAsync(update, handler, services, context.RequestAborted).ConfigureAwait(false);
        await context.Response.WriteAsJsonAsync(new { success = true }, context.RequestAborted).ConfigureAwait(false);
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
