using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Max.Bot.Api;
using Max.Bot.Configuration;
using Max.Bot.Networking;
using Max.Bot.Polling;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Max.Bot.Types.Requests;
using Microsoft.Extensions.Logging;

namespace Max.Bot;

/// <summary>
/// Main client for interacting with the Max Messenger Bot API.
/// </summary>
public class MaxClient : IMaxBotApi, IUpdatePipeline, IDisposable
{
    private readonly IMaxHttpClient _apiClient;
    private readonly IMaxHttpClient _pollClient;
    private readonly bool _ownsApiClient;
    private readonly bool _ownsPollClient;
    private readonly MaxBotOptions _options;
    private readonly ILogger<MaxClient>? _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly HashSet<UpdateType>? _dispatchTypeFilter;
    private readonly HashSet<string>? _allowedUsernames;
    private readonly object _pollerLock = new();
    private UpdatePoller? _updatePoller;

    /// <summary>
    /// Gets the options this client was created with.
    /// </summary>
    internal MaxBotOptions Options => _options;

    /// <inheritdoc />
    public IBotApi Bot { get; }

    /// <inheritdoc />
    public IMessagesApi Messages { get; }

    /// <inheritdoc />
    public IChatsApi Chats { get; }

    /// <inheritdoc />
    public IUsersApi Users { get; }

    /// <inheritdoc />
    public IFilesApi Files { get; }

    /// <inheritdoc />
    public ISubscriptionsApi Subscriptions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxClient"/> class with a bot token.
    /// </summary>
    /// <param name="token">The bot token for authentication.</param>
    /// <exception cref="ArgumentException">Thrown when token is null or empty, or options are invalid.</exception>
    public MaxClient(string token)
        : this(new MaxBotOptions { Token = token })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxClient"/> class with bot options.
    /// </summary>
    /// <param name="options">The bot options containing token and base URL.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when options are invalid.</exception>
    public MaxClient(MaxBotOptions options)
        : this(options, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxClient"/> class with bot options and optional dependencies.
    /// </summary>
    /// <param name="options">The bot options containing token and base URL.</param>
    /// <param name="httpClient">Optional HTTP client for API requests. If null, a new HttpClient will be created. Its Timeout is respected and never overwritten.</param>
    /// <param name="pollingHttpClient">Optional HTTP client dedicated to long polling (GET /updates). If null, a new HttpClient will be created with timeout matching <see cref="MaxPollingOptions.LongPollingTimeout"/>.</param>
    /// <param name="logger">Optional logger for logging events.</param>
    /// <param name="loggerFactory">Optional logger factory used to create component-specific loggers.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when options are invalid.</exception>
    public MaxClient(MaxBotOptions options, HttpClient? httpClient = null, HttpClient? pollingHttpClient = null, ILogger<MaxClient>? logger = null, ILoggerFactory? loggerFactory = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _logger = logger;
        _loggerFactory = loggerFactory;
        _dispatchTypeFilter = UpdateFilterUtilities.BuildTypeFilter(_options);
        _allowedUsernames = UpdateFilterUtilities.BuildAllowedUsernames(_options);

        // --- API client (all methods except long polling) ---
        _ownsApiClient = httpClient == null;
        var apiHttpClient = httpClient ?? new HttpClient();

        // Respect the user-provided HttpClient.Timeout. Only set a default if we created the client ourselves.
        if (httpClient == null)
        {
            apiHttpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        var apiClientOptions = new MaxBotClientOptions
        {
            BaseUrl = _options.BaseUrl,
            Timeout = apiHttpClient.Timeout,
            RetryCount = 3,
            EnableDetailedLogging = true
        };
        apiClientOptions.Validate();
        _apiClient = new MaxHttpClient(apiHttpClient, apiClientOptions, loggerFactory?.CreateLogger<MaxHttpClient>());

        // --- Polling client (GET /updates only) ---
        _ownsPollClient = pollingHttpClient == null;
        var pollHttpClient = pollingHttpClient ?? new HttpClient();

        if (pollingHttpClient == null)
        {
            // Long polling timeout + 10s buffer for network delays
            pollHttpClient.Timeout = _options.Polling.LongPollingTimeout + TimeSpan.FromSeconds(10);
        }

        var pollClientOptions = new MaxBotClientOptions
        {
            BaseUrl = _options.BaseUrl,
            Timeout = pollHttpClient.Timeout,
            RetryCount = 3,
            EnableDetailedLogging = true
        };
        pollClientOptions.Validate();
        _pollClient = new MaxHttpClient(pollHttpClient, pollClientOptions, loggerFactory?.CreateLogger<MaxHttpClient>());

        // Initialize API groups — all use the api client
        Bot = new BotApi(_apiClient, _options);
        Messages = new MessagesApi(_apiClient, _options);
        Chats = new ChatsApi(_apiClient, _options);
        Users = new UsersApi(_apiClient, _options);
        Files = new FilesApi(_apiClient, _options);
        Subscriptions = new SubscriptionsApi(_apiClient, _options);
    }

    /// <summary>
    /// Starts the long polling loop with the provided <see cref="IUpdateHandler"/>.
    /// </summary>
    public async Task StartPollingAsync(IUpdateHandler handler, IServiceProvider? services = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_pollerLock)
        {
            if (_updatePoller != null)
            {
                throw new InvalidOperationException("Polling is already running. Call StopPollingAsync before starting again.");
            }

            _updatePoller = new UpdatePoller(
                this,
                Subscriptions,
                _options,
                _pollClient,
                _loggerFactory?.CreateLogger<UpdatePoller>(),
                services);
        }

        await _updatePoller.StartAsync(handler, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the long polling loop if it is running.
    /// </summary>
    public async Task StopPollingAsync(CancellationToken cancellationToken = default)
    {
        UpdatePoller? poller;
        lock (_pollerLock)
        {
            poller = _updatePoller;
            _updatePoller = null;
        }

        if (poller != null)
        {
            await poller.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Processes a webhook update payload using the provided handler.
    /// </summary>
    public async Task ProcessWebhookAsync(Update update, IUpdateHandler handler, IServiceProvider? services = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(handler);

        if (!UpdateFilterUtilities.ShouldDispatch(update, _dispatchTypeFilter, _allowedUsernames))
        {
            _logger?.LogDebug("Webhook update {UpdateId} skipped by filters.", update.UpdateId);
            return;
        }

        await UpdateHandlerExecutor.ExecuteAsync(update, handler, _options, this, _logger, services, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Configures the webhook endpoint by calling <c>POST /subscriptions</c>.
    /// </summary>
    /// <param name="url">
    /// The webhook URL where updates will be sent. Optional: when omitted, <see cref="MaxWebhookOptions.Endpoint"/>
    /// from <see cref="MaxBotOptions.Webhook"/> is used.
    /// </param>
    /// <param name="updateTypes">Optional list of update types to receive. If null, all update types will be received.</param>
    /// <param name="secret">
    /// Optional secret that MAX sends back in the <c>X-Max-Bot-Api-Secret</c> header. When omitted,
    /// <see cref="MaxWebhookOptions.SecretToken"/> is used; when provided, it replaces <see cref="MaxWebhookOptions.SecretToken"/>
    /// after a successful subscription so that incoming webhooks are validated against the secret MAX actually sends.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with success status.</returns>
    /// <exception cref="ArgumentException">Thrown when neither url nor <see cref="MaxWebhookOptions.Endpoint"/> is set, the URL is invalid, or the secret does not match the MAX format.</exception>
    public async Task<Response> ConfigureWebhookAsync(string? url = null, List<UpdateType>? updateTypes = null, string? secret = null, CancellationToken cancellationToken = default)
    {
        var webhookUrl = string.IsNullOrWhiteSpace(url) ? _options.Webhook.Endpoint : url;
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new ArgumentException("Webhook URL must be provided either as an argument or via MaxBotOptions.Webhook.Endpoint.", nameof(url));
        }

        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var parsed))
        {
            throw new ArgumentException("Webhook URL must be an absolute URI.", nameof(url));
        }

        if (_options.Webhook.EnforceHttps && !Uri.UriSchemeHttps.Equals(parsed.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Webhook URL must use HTTPS when EnforceHttps is enabled.", nameof(url));
        }

        var webhookSecret = string.IsNullOrWhiteSpace(secret) ? _options.Webhook.SecretToken : secret;
        if (!string.IsNullOrWhiteSpace(webhookSecret) && !MaxWebhookOptions.IsValidSecret(webhookSecret))
        {
            throw new ArgumentException("Webhook secret must be 5-256 characters long and contain only A-Z, a-z, 0-9, '_' and '-'.", nameof(secret));
        }

        var request = new SetWebhookRequest
        {
            Url = webhookUrl,
            UpdateTypes = updateTypes,
            Secret = string.IsNullOrWhiteSpace(webhookSecret) ? null : webhookSecret
        };

        var response = await Subscriptions.SetWebhookAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.Success)
        {
            return response;
        }

        // Keep incoming webhook validation in sync with the subscription that MAX now uses.
        _options.Webhook.Endpoint = webhookUrl;
        if (!string.IsNullOrWhiteSpace(secret))
        {
            _options.Webhook.SecretToken = secret;
        }

        return response;
    }

    /// <summary>
    /// Deletes the webhook subscription by calling <c>DELETE /subscriptions</c>.
    /// </summary>
    /// <param name="url">
    /// The webhook URL to remove from subscriptions. Optional: when omitted, <see cref="MaxWebhookOptions.Endpoint"/> is used.
    /// </param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with success status.</returns>
    /// <exception cref="ArgumentException">Thrown when neither url nor <see cref="MaxWebhookOptions.Endpoint"/> is set.</exception>
    public Task<Response> DeleteWebhookAsync(string? url = null, CancellationToken cancellationToken = default)
    {
        var webhookUrl = string.IsNullOrWhiteSpace(url) ? _options.Webhook.Endpoint : url;
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new ArgumentException("Webhook URL must be provided either as an argument or via MaxBotOptions.Webhook.Endpoint.", nameof(url));
        }

        return Subscriptions.DeleteWebhookAsync(webhookUrl, cancellationToken);
    }

    /// <summary>
    /// Disposes the HTTP clients that were created internally by MaxClient.
    /// User-supplied clients are never disposed.
    /// </summary>
    public void Dispose()
    {
        if (_ownsApiClient)
            _apiClient?.Dispose();
        if (_ownsPollClient)
            _pollClient?.Dispose();
    }
}


