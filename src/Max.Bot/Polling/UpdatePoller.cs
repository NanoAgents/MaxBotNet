using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Max.Bot.Api;
using Max.Bot.Configuration;
using Max.Bot.Exceptions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Max.Bot.Types.Helpers;
using Max.Bot.Types.Requests;
using Microsoft.Extensions.Logging;

namespace Max.Bot.Polling;

/// <summary>
/// Implements resilient long polling against the MAX Bot API.
/// </summary>
public sealed class UpdatePoller : IAsyncDisposable
{
    private readonly IMaxBotApi _api;
    private readonly IMaxHttpClient _pollClient;
    private readonly MaxBotOptions _options;
    private readonly ILogger<UpdatePoller>? _logger;
    private readonly IServiceProvider? _serviceProvider;
    private readonly object _lifecycleLock = new();
    private readonly List<Task> _inFlightHandlers = new();
    private readonly HashSet<UpdateType>? _handlingTypeFilter;
    private readonly List<UpdateType>? _typeQueryFilter;
    private readonly HashSet<string>? _allowedUsernames;
    private readonly string _token;
    private readonly bool _ownsPollClient;

    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private IUpdateHandler? _handler;
    private long? _marker;
    private int _consecutiveErrors;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatePoller"/> class.
    /// </summary>
    /// <param name="api">The bot API surface used for non-polling requests.</param>
    /// <param name="subscriptionsApi">Reserved for backward compatibility; currently unused as polling uses its own HTTP client.</param>
    /// <param name="options">Bot options containing token, base URL, and polling settings.</param>
    /// <param name="pollClient">HTTP client dedicated to long polling. If null, a default client will be created internally.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="serviceProvider">Optional service provider for handler resolution.</param>
    public UpdatePoller(
        IMaxBotApi api,
        ISubscriptionsApi subscriptionsApi,
        MaxBotOptions options,
        IMaxHttpClient? pollClient = null,
        ILogger<UpdatePoller>? logger = null,
        IServiceProvider? serviceProvider = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _ = subscriptionsApi ?? throw new ArgumentNullException(nameof(subscriptionsApi));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _serviceProvider = serviceProvider;
        _token = options.Token;

        // Create default polling client if none provided
        if (pollClient == null)
        {
            var defaultClient = new System.Net.Http.HttpClient
            {
                Timeout = _options.Polling.LongPollingTimeout + TimeSpan.FromSeconds(10)
            };
            var clientOptions = new MaxBotClientOptions
            {
                BaseUrl = _options.BaseUrl,
                Timeout = defaultClient.Timeout,
                RetryCount = 3,
                EnableDetailedLogging = false
            };
            _pollClient = new MaxHttpClient(defaultClient, clientOptions);
            _ownsPollClient = true;
        }
        else
        {
            _pollClient = pollClient;
            _ownsPollClient = false;
        }

        _marker = _options.Polling.InitialMarker;
        _handlingTypeFilter = UpdateFilterUtilities.BuildTypeFilter(options);
        _typeQueryFilter = BuildTypeQueryFilter(options);
        _allowedUsernames = UpdateFilterUtilities.BuildAllowedUsernames(options);
    }

    /// <summary>
    /// Starts the polling loop.
    /// </summary>
    public Task StartAsync(IUpdateHandler handler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_lifecycleLock)
        {
            if (_pollingTask is { IsCompleted: false })
            {
                throw new InvalidOperationException("UpdatePoller is already running.");
            }

            _handler = handler;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _pollingTask = Task.Run(() => PollAsync(_cts.Token), CancellationToken.None);
        }

        _logger?.LogInformation(
            "UpdatePoller started with timeout {TimeoutSeconds}s and limit {Limit}.",
            _options.Polling.LongPollingTimeout.TotalSeconds,
            _options.Polling.BatchSize);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the polling loop and waits for in-flight handlers to complete.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Task? runningTask = null;

        lock (_lifecycleLock)
        {
            if (_cts == null)
            {
                return;
            }

            _cts.Cancel();
            runningTask = _pollingTask;
        }

        if (runningTask != null)
        {
            try
            {
                await runningTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                // * Poller cancellation requested: swallow to ensure graceful shutdown
            }
        }

        await DrainHandlersAsync(cancellationToken).ConfigureAwait(false);

        lock (_lifecycleLock)
        {
            _cts?.Dispose();
            _cts = null;
            _pollingTask = null;
            _handler = null;
        }

        _logger?.LogInformation("UpdatePoller stopped.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);

        // Dispose the polling client only if we created it internally
        if (_ownsPollClient)
        {
            _pollClient?.Dispose();
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await FetchUpdatesAsync(_marker, cancellationToken).ConfigureAwait(false);
                _consecutiveErrors = 0;

                if (response.Updates?.Length > 0)
                {
                    var batchCompleted = true;
                    foreach (var update in response.Updates)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            batchCompleted = false;
                            break;
                        }

                        if (!ShouldDispatch(update))
                        {
                            continue;
                        }

                        try
                        {
                            await DispatchAsync(update, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Failed to dispatch update {UpdateId}. Skipping.", update.UpdateId);
                        }
                    }

                    // * MAX updates carry no update_id: the next page is addressed only by the marker returned with the batch.
                    // Advance it only when the whole batch was walked, so a stop mid-batch re-fetches the rest (at-least-once).
                    if (batchCompleted)
                    {
                        _marker = _options.Polling.PersistMarkers
                            ? response.Marker ?? _marker
                            : response.Marker ?? _options.Polling.InitialMarker;
                    }
                }
                else
                {
                    await Task.Delay(_options.Polling.IdleDelay, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (MaxRateLimitException ex)
            {
                await HandleTransientErrorAsync(ex, cancellationToken, "Rate limit reached while polling updates.").ConfigureAwait(false);
            }
            catch (MaxNetworkException ex)
            {
                await HandleTransientErrorAsync(ex, cancellationToken, "Network error while polling updates.").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await HandleTransientErrorAsync(ex, cancellationToken, "Unexpected error in UpdatePoller.").ConfigureAwait(false);
            }
        }

        // * The poll token is already cancelled here: wait for the handlers themselves, StopAsync bounds the wait.
        await DrainHandlersAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task<GetUpdatesResponse> FetchUpdatesAsync(long? marker, CancellationToken cancellationToken)
    {
        // Build query parameters for GET /updates.
        // This mirrors SubscriptionsApi.GetUpdatesAsync but executes against
        // the dedicated polling client with its own (larger) timeout.
        var queryParams = new Dictionary<string, string?>
        {
            { "limit", _options.Polling.BatchSize.ToString(CultureInfo.InvariantCulture) },
            { "timeout", ((int)Math.Round(_options.Polling.LongPollingTimeout.TotalSeconds)).ToString(CultureInfo.InvariantCulture) }
        };

        if (marker.HasValue)
        {
            queryParams["marker"] = marker.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (_typeQueryFilter != null && _typeQueryFilter.Count > 0)
        {
            queryParams["types"] = string.Join(",", _typeQueryFilter.Select(UpdateTypeHelper.ToStringValue));
        }

        var apiRequest = new MaxApiRequest
        {
            Method = HttpMethod.Get,
            Endpoint = "/updates",
            QueryParameters = queryParams,
            Headers = new Dictionary<string, string?>
            {
                ["Authorization"] = _token
            }
        };

        string body;
        try
        {
            body = await _pollClient.SendAsyncRaw(apiRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (MaxApiException)
        {
            throw; // Let the caller's catch-all handle it
        }
        catch (Exception ex)
        {
            throw new MaxNetworkException($"Failed to fetch updates: {ex.Message}", ex);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new MaxApiException(
                "Polling request returned empty response.",
                null,
                System.Net.HttpStatusCode.BadRequest);
        }

        try
        {
            return GetUpdatesResponse.ParseTolerant(body, (ex, raw) =>
                _logger?.LogError(ex, "Skipping update that could not be deserialized: {Update}", raw));
        }
        catch (Exception ex)
        {
            throw new MaxNetworkException($"Failed to deserialize polling response: {ex.Message}", ex);
        }
    }

    private bool ShouldDispatch(Update update)
    {
        return UpdateFilterUtilities.ShouldDispatch(update, _handlingTypeFilter, _allowedUsernames);
    }

    private async Task DispatchAsync(Update update, CancellationToken cancellationToken)
    {
        if (_options.Handling.PreserveUpdateOrder || _options.Handling.MaxDegreeOfParallelism <= 1)
        {
            await HandleUpdateInternalAsync(update, cancellationToken).ConfigureAwait(false);
            return;
        }

        var handlerTask = HandleUpdateInternalAsync(update, cancellationToken)
            .ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    _logger?.LogError(t.Exception, "Unhandled exception in update handler for update {UpdateId}.", update.UpdateId);
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);

        _inFlightHandlers.Add(handlerTask);

        if (_inFlightHandlers.Count >= _options.Handling.MaxDegreeOfParallelism)
        {
            var completed = await Task.WhenAny(_inFlightHandlers).ConfigureAwait(false);
            _inFlightHandlers.Remove(completed);
            await completed.ConfigureAwait(false);
        }

        _inFlightHandlers.RemoveAll(static task => task.IsCompleted);
    }

    private Task HandleUpdateInternalAsync(Update update, CancellationToken cancellationToken)
    {
        var handler = _handler ?? throw new InvalidOperationException("UpdatePoller has not been started.");
        return UpdateHandlerExecutor.ExecuteAsync(update, handler, _options, _api, _logger, _serviceProvider, cancellationToken);
    }

    private async Task HandleTransientErrorAsync(Exception exception, CancellationToken cancellationToken, string message)
    {
        _consecutiveErrors++;
        var delay = NextBackoffDelay();
        _logger?.LogWarning(exception, "{Message} Retrying in {Delay}ms.", message, delay.TotalMilliseconds);
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    }

    private TimeSpan NextBackoffDelay()
    {
        var backoffBase = _options.Polling.ErrorBackoffBase.TotalMilliseconds;
        var capped = Math.Min(
            backoffBase * Math.Pow(2, Math.Min(_consecutiveErrors, 10)),
            _options.Polling.ErrorBackoffMax.TotalMilliseconds);
        var jitter = 1 + ((Random.Shared.NextDouble() - 0.5) * 0.2); // +/-10%
        var delayMs = Math.Max(backoffBase, capped * jitter);
        return TimeSpan.FromMilliseconds(delayMs);
    }

    private async Task DrainHandlersAsync(CancellationToken cancellationToken)
    {
        if (_inFlightHandlers.Count == 0)
        {
            return;
        }

        var handlers = _inFlightHandlers.ToArray();
        _inFlightHandlers.Clear();
        await Task.WhenAll(handlers.Select(task => task.WaitAsync(cancellationToken))).ConfigureAwait(false);
    }

    private static List<UpdateType>? BuildTypeQueryFilter(MaxBotOptions options)
    {
        ICollection<UpdateType>? candidates = options.Polling.AllowedUpdateTypes is { Count: > 0 }
            ? options.Polling.AllowedUpdateTypes
            : options.Handling.AllowedUpdateTypes;

        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        return candidates.Distinct().ToList();
    }
}



