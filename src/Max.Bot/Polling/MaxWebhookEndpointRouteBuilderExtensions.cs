using System;
using System.Threading.Tasks;
using Max.Bot.Configuration;
using Max.Bot.Polling;
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
    /// Requires <see cref="IUpdatePipeline"/> (or <see cref="Max.Bot.MaxClient"/>) and <see cref="IUpdateHandler"/> in the DI container.
    /// Options are taken from a registered <see cref="MaxBotOptions"/>, otherwise from the <see cref="Max.Bot.MaxClient"/> pipeline,
    /// otherwise from a configured <c>IOptions&lt;MaxBotOptions&gt;</c> (with a non-empty token).
    /// When <see cref="MaxWebhookOptions.SecretToken"/> is set, requests without a matching
    /// <see cref="MaxWebhookOptions.SignatureHeaderName"/> header (<c>X-Max-Bot-Api-Secret</c> by default) are rejected with 401
    /// before the body is read; bodies larger than <see cref="MaxWebhookOptions.MaxBodySizeKilobytes"/> are rejected with 413.
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
        var pipeline = services.GetService<IUpdatePipeline>()
            ?? services.GetService<Max.Bot.MaxClient>()
            ?? throw new InvalidOperationException("IUpdatePipeline (for example MaxClient) is not registered in the service container.");
        var handler = services.GetService<IUpdateHandler>()
            ?? throw new InvalidOperationException("IUpdateHandler is not registered in the service container.");
        var options = ResolveOptions(services, pipeline);
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Max.Bot.Webhook");

        var statusCode = await WebhookRequestProcessor.ProcessAsync(context, options, pipeline, handler, logger).ConfigureAwait(false);
        context.Response.StatusCode = statusCode;
        if (statusCode == StatusCodes.Status200OK)
        {
            await context.Response.WriteAsJsonAsync(new { success = true }, context.RequestAborted).ConfigureAwait(false);
        }
    }

    private static MaxBotOptions ResolveOptions(IServiceProvider services, IUpdatePipeline pipeline)
    {
        var options = services.GetService<MaxBotOptions>();
        if (options != null)
        {
            return options;
        }

        if (pipeline is Max.Bot.MaxClient client)
        {
            return client.Options;
        }

        // * IOptions<T> always resolves (it is registered as an open generic): an unconfigured instance would
        // silently disable the secret check, so only accept options that were actually configured.
        var configured = services.GetService<IOptions<MaxBotOptions>>()?.Value;
        if (configured != null && !string.IsNullOrWhiteSpace(configured.Token))
        {
            return configured;
        }

        throw new InvalidOperationException("MaxBotOptions is not registered in the service container.");
    }
}
