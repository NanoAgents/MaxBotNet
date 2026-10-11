using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Max.Bot.Configuration;
using Max.Bot.Exceptions;
using Max.Bot.Types;
using Microsoft.Extensions.Logging;

namespace Max.Bot.Networking;

/// <summary>
/// HTTP client implementation for communicating with the Max Bot API.
/// </summary>
public class MaxHttpClient : IMaxHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly MaxBotClientOptions _options;
    private readonly ILogger<MaxHttpClient>? _logger;

    /// <summary>
    /// Gets the User-Agent header value sent with every API request (MAX API requires it).
    /// </summary>
    internal static string UserAgent { get; } = BuildUserAgent();

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxHttpClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="options">The options.</param>
    /// <param name="logger">The logger.</param>
    public MaxHttpClient(HttpClient httpClient, MaxBotClientOptions options, ILogger<MaxHttpClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        _options.Validate();

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = _options.Timeout;
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Sends the request and deserializes the response.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    public async Task<TResponse> SendAsync<TResponse>(MaxApiRequest request, CancellationToken cancellationToken = default)
    {
        var responseBody = await SendAsyncRaw(request, cancellationToken).ConfigureAwait(false);
        try
        {
            return MaxJsonSerializer.Deserialize<TResponse>(responseBody);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to deserialize response. Body: {ResponseBody}", responseBody);
            throw new MaxNetworkException($"Failed to deserialize API response: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Sends the request without deserializing the response.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendAsync(MaxApiRequest request, CancellationToken cancellationToken = default)
    {
        await SendAsyncRaw(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the request and returns the raw response body.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The raw response body.</returns>
    public async Task<string> SendAsyncRaw(MaxApiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= _options.RetryCount)
        {
            try
            {
                using var httpRequest = BuildHttpRequestMessage(request, cancellationToken);
                _logger?.LogDebug("Sending HTTP {Method} {Endpoint}", request.Method, request.Endpoint);

                if (_options.EnableDetailedLogging && httpRequest.Content != null)
                {
                    var requestBody = await httpRequest.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    _logger?.LogTrace("Request body: {Body}", requestBody);
                }

                return await ExecuteWithResponseHandlingAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ShouldRetry(ex, attempt, request) || attempt == _options.RetryCount)
            {
                lastException = ex;
                attempt++;
                _logger?.LogWarning(ex, "HTTP request failed on attempt {Attempt} of {MaxAttempts}", attempt, _options.RetryCount + 1);
                if (attempt > _options.RetryCount) break;
                _logger?.LogInformation("Retrying HTTP request, attempt {Attempt} of {MaxAttempts}", attempt + 1, _options.RetryCount + 1);
                await Task.Delay(CalculateRetryDelay(ex, attempt), cancellationToken).ConfigureAwait(false);
            }
        }

        if (lastException != null)
        {
            _logger?.LogError(lastException, "HTTP request retry attempts failed");
            throw lastException;
        }

        throw new MaxNetworkException("Request failed after retries.");
    }

    /// <summary>
    /// Sends a raw request to an absolute URL.
    /// </summary>
    /// <param name="absoluteUrl">The absolute URL.</param>
    /// <param name="contentFactory">The content factory.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="method">The HTTP method.</param>
    /// <returns>The response body.</returns>
    public async Task<string> SendAsyncRaw(
        string absoluteUrl,
        Func<HttpContent?>? contentFactory = null,
        CancellationToken cancellationToken = default,
        HttpMethod? method = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absoluteUrl);

        // SSRF Protection: only allow http/https
        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException("Invalid or unsafe upload URL. Only HTTP/HTTPS are allowed.", nameof(absoluteUrl));
        }

        var requestMethod = method ?? HttpMethod.Post;
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= _options.RetryCount)
        {
            try
            {
                using var httpRequest = new HttpRequestMessage(requestMethod, absoluteUrl);

                // Upload URLs returned by POST /uploads are pre-signed, so no Authorization header is needed.
                if (contentFactory != null)
                {
                    httpRequest.Content = contentFactory();
                }

                return await ExecuteWithResponseHandlingAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ShouldRetry(ex, attempt) || attempt == _options.RetryCount)
            {
                lastException = ex;
                attempt++;
                _logger?.LogWarning(ex, "HTTP request failed on attempt {Attempt} of {MaxAttempts}", attempt, _options.RetryCount + 1);
                if (attempt > _options.RetryCount) break;
                _logger?.LogInformation("Retrying HTTP request, attempt {Attempt} of {MaxAttempts}", attempt + 1, _options.RetryCount + 1);
                await Task.Delay(CalculateRetryDelay(ex, attempt), cancellationToken).ConfigureAwait(false);
            }
        }

        if (lastException != null)
        {
            _logger?.LogError(lastException, "HTTP request retry attempts failed");
            throw lastException;
        }

        throw new MaxNetworkException("Request to absolute URL failed.");
    }

    private async Task<string> ExecuteWithResponseHandlingAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        string? responseBody = null;
        if (response.Content != null)
        {
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_options.EnableDetailedLogging)
        {
            _logger?.LogTrace("HTTP {Method} {Url} -> {StatusCode} ({ElapsedMs}ms)\nResponse body: {Body}",
                request.Method, request.RequestUri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, responseBody ?? "(null)");
        }
        else
        {
            _logger?.LogDebug("HTTP request succeeded with status {StatusCode} for {Method} {Url} ({ElapsedMs}ms)",
                (int)response.StatusCode, request.Method, request.RequestUri, stopwatch.ElapsedMilliseconds);
        }

        if (!response.IsSuccessStatusCode)
        {
            HandleHttpResponseError(response, responseBody ?? string.Empty);
        }

        return responseBody ?? string.Empty;
    }

    private HttpRequestMessage BuildHttpRequestMessage(MaxApiRequest request, CancellationToken cancellationToken)
    {
        var url = request.BuildUrl(_options.BaseUrl);
        var httpRequest = new HttpRequestMessage(request.Method, url);

        // MAX API requires User-Agent header (same as Go reference client)
        httpRequest.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

        if (request.Headers != null)
        {
            foreach (var header in request.Headers)
            {
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                    httpRequest.Headers.TryAddWithoutValidation("Authorization", header.Value);
                else if (!header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (request.Body != null && request.Method != HttpMethod.Get)
        {
            var json = MaxJsonSerializer.Serialize(request.Body);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return httpRequest;
    }

    private void HandleHttpResponseError(HttpResponseMessage response, string? responseBody)
    {
        var (errorCode, errorMessage) = ParseErrorBody(responseBody);

        errorMessage ??= string.IsNullOrWhiteSpace(responseBody)
            ? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
            : responseBody;

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new MaxUnauthorizedException(errorMessage, errorCode, response.StatusCode),
            HttpStatusCode.TooManyRequests => new MaxRateLimitException(errorMessage, errorCode, response.StatusCode, GetRetryAfter(response)),
            HttpStatusCode.RequestTimeout => new MaxNetworkException(errorMessage, errorCode, response.StatusCode),
            _ when (int)response.StatusCode >= 500 => new MaxNetworkException(errorMessage, errorCode, response.StatusCode),
            _ when MaxAttachmentNotReadyException.IsAttachmentNotReadyCode(errorCode) => new MaxAttachmentNotReadyException(errorMessage, errorCode, response.StatusCode),
            _ => new MaxApiException(errorMessage, errorCode, response.StatusCode),
        };
    }

    /// <summary>
    /// Extracts the error code and message from an error response body.
    /// MAX API returns errors as <c>{"code": "...", "message": "..."}</c>; the legacy
    /// <c>{"ok": false, "error": {"code": "...", "message": "..."}}</c> envelope is still supported.
    /// </summary>
    internal static (string? Code, string? Message) ParseErrorBody(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return (null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return (null, null);
            }

            if (root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.Object)
            {
                var nestedCode = GetString(errorElement, "code");
                var nestedMessage = GetString(errorElement, "message");
                if (nestedCode != null || nestedMessage != null)
                {
                    return (nestedCode, nestedMessage);
                }
            }

            var code = GetString(root, "code");
            var message = GetString(root, "message");
            if (code == null && message == null && root.TryGetProperty("error", out var errorText) && errorText.ValueKind == JsonValueKind.String)
            {
                message = errorText.GetString();
            }

            return (code, message);
        }
        catch (JsonException)
        {
            return (null, null);
        }

        static string? GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
    }

    private TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var value = values.FirstOrDefault();
            if (int.TryParse(value, out var seconds)) return TimeSpan.FromSeconds(seconds);
            if (DateTimeOffset.TryParse(value, out var date)) return date - DateTimeOffset.UtcNow;
        }
        return null;
    }

    private bool ShouldRetry(Exception ex, int attempt, MaxApiRequest? request = null)
    {
        if (attempt >= _options.RetryCount) return false;

        if (request != null && request.Method == HttpMethod.Get && request.Endpoint.EndsWith("/updates"))
            return false; // Don't retry long polling timeouts

        return ex is MaxNetworkException || ex is MaxRateLimitException || ex is HttpRequestException || ex is TaskCanceledException;
    }

    private TimeSpan CalculateRetryDelay(Exception ex, int attempt)
    {
        if (ex is MaxRateLimitException rex && rex.RetryAfter.HasValue)
            return rex.RetryAfter.Value > _options.MaxRetryDelay ? _options.MaxRetryDelay : rex.RetryAfter.Value;

        var delay = TimeSpan.FromMilliseconds(_options.RetryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
        var jitter = 0.8 + (new Random().NextDouble() * 0.4);
        delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * jitter);
        return delay > _options.MaxRetryDelay ? _options.MaxRetryDelay : delay;
    }

    private static string BuildUserAgent()
    {
        var assembly = typeof(MaxHttpClient).Assembly;
        var version = assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        // Strip SourceLink commit metadata (e.g. "0.7.0-alpha+abc123").
        var metadataIndex = version.IndexOf('+');
        if (metadataIndex >= 0)
        {
            version = version[..metadataIndex];
        }

        return $"MaxMessenger.Bot/{version}";
    }

    /// <summary>
    /// Disposes the HTTP client.
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
