using System;
using System.Threading;
using System.Threading.Tasks;
using Max.Bot.Configuration;
using Max.Bot.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Max.Bot.Polling;

/// <summary>
/// Base ASP.NET Core controller that receives webhook callbacks from MAX.
/// </summary>
/// <remarks>
/// <para>
/// The controller is abstract, so referencing the library no longer adds a <c>POST /api/max/webhook</c> endpoint
/// to every ASP.NET Core application. To receive webhooks either map the minimal endpoint with
/// <c>app.MapMaxWebhook()</c>, or opt in to the controller by deriving from it:
/// </para>
/// <code>
/// [Route("api/max/webhook")]
/// public sealed class MaxWebhookController : WebhookController
/// {
///     public MaxWebhookController(IUpdatePipeline pipeline, IUpdateHandler handler, MaxBotOptions options, ILogger&lt;WebhookController&gt;? logger = null)
///         : base(pipeline, handler, options, logger)
///     {
///     }
/// }
/// </code>
/// <para>
/// <see cref="IUpdatePipeline"/> (for example a singleton <see cref="MaxClient"/>), <see cref="IUpdateHandler"/>
/// and <see cref="MaxBotOptions"/> must be registered in the DI container.
/// </para>
/// </remarks>
[ApiController]
[Route("api/max/webhook")]
public abstract class WebhookController : ControllerBase
{
    private readonly IUpdatePipeline _pipeline;
    private readonly IUpdateHandler _handler;
    private readonly MaxBotOptions _options;
    private readonly ILogger<WebhookController>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookController"/> class.
    /// </summary>
    protected WebhookController(IUpdatePipeline pipeline, IUpdateHandler handler, MaxBotOptions options, ILogger<WebhookController>? logger = null)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <summary>
    /// Handles incoming webhook updates.
    /// </summary>
    [HttpPost]
    public virtual async Task<IActionResult> PostAsync([FromBody] Update update, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!WebhookRequestValidator.ValidatePayloadSize(Request, _options, _logger))
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        if (!WebhookRequestValidator.ValidateSignature(Request, _options, _logger))
        {
            return Unauthorized();
        }

        await _pipeline.ProcessWebhookAsync(update, _handler, HttpContext.RequestServices, cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true });
    }
}
