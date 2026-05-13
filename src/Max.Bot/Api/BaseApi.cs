using System.Net;
using System.Net.Http;
using System.Text.Json;
using Max.Bot.Configuration;
using Max.Bot.Exceptions;
using Max.Bot.Networking;
using Max.Bot.Types;

namespace Max.Bot.Api;

/// <summary>
/// Base class for API classes that provides common functionality.
/// </summary>
internal abstract class BaseApi
{
    private static readonly Type ResponseType = typeof(Response);
    private static readonly Type GetUpdatesResponseType = typeof(GetUpdatesResponse);

    /// <summary>
    /// Gets the HTTP client for making API requests.
    /// </summary>
    protected readonly IMaxHttpClient HttpClient;

    /// <summary>
    /// Gets the bot options containing token and base URL.
    /// </summary>
    protected readonly MaxBotOptions Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseApi"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    /// <param name="options">The bot options containing token and base URL.</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient or options is null.</exception>
    protected BaseApi(IMaxHttpClient httpClient, MaxBotOptions options)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Builds the endpoint URL path.
    /// </summary>
    /// <param name="path">The endpoint path (e.g., "/me", "/messages").</param>
    /// <returns>The normalized endpoint path (e.g., "/me", "/messages").</returns>
    /// <exception cref="ArgumentException">Thrown when path is null or empty.</exception>
    protected string BuildEndpoint(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        // Ensure path starts with '/'
        // Token is passed via Authorization header, not in URL path
        return path.StartsWith('/') ? path : $"/{path}";
    }

    /// <summary>
    /// Creates a MaxApiRequest with Authorization header and proper endpoint.
    /// </summary>
    /// <param name="method">The HTTP method (GET, POST, PUT, DELETE, etc.).</param>
    /// <param name="endpoint">The endpoint path relative to base URL (without token).</param>
    /// <param name="body">The request body object to be serialized to JSON. Can be null.</param>
    /// <param name="queryParams">The query parameters dictionary. Can be null.</param>
    /// <returns>A MaxApiRequest ready to be sent.</returns>
    protected MaxApiRequest CreateRequest(
        HttpMethod method,
        string endpoint,
        object? body = null,
        Dictionary<string, string?>? queryParams = null)
    {
        var request = new MaxApiRequest
        {
            Method = method,
            Endpoint = BuildEndpoint(endpoint),
            Body = body,
            QueryParameters = queryParams
        };

        // Add Authorization header with token (format: "Authorization: <token>" as per Max API docs)
        request.Headers ??= new Dictionary<string, string?>();
        request.Headers["Authorization"] = Options.Token;

        return request;
    }

    /// <summary>
    /// Executes a request and handles Response&lt;T&gt; wrapper.
    /// </summary>
    /// <typeparam name="T">The type of the response data.</typeparam>
    /// <param name="request">The API request to send.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unwrapped response data.</returns>
    /// <exception cref="MaxApiException">Thrown when the API returns an error response (ok: false or result is null).</exception>
    /// <exception cref="MaxNetworkException">Thrown when a network error occurs.</exception>
    /// <exception cref="MaxUnauthorizedException">Thrown when authentication fails.</exception>
    protected async Task<T> ExecuteRequestAsync<T>(
        MaxApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var responseType = typeof(T);

        // Special case: if T is Response (simple response without Result wrapper), deserialize directly
        if (responseType == ResponseType)
        {
            var simpleResponse = await HttpClient.SendAsync<Response>(request, cancellationToken).ConfigureAwait(false);
            if (simpleResponse == null)
            {
                throw new MaxApiException(
                    "API request returned null response.",
                    null,
                    HttpStatusCode.BadRequest);
            }
            return (T)(object)simpleResponse;
        }

        // For all requests: use SendAsyncRaw to get the response body once, then try both deserialization strategies.
        // This prevents making duplicate HTTP calls (which could cause duplicate actions for POST/PUT/PATCH).
        if (request.Method == HttpMethod.Get)
        {
            // Get raw response body once
            var responseBody = await HttpClient.SendAsyncRaw(request, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new MaxApiException(
                    "API request returned empty response body.",
                    null,
                    HttpStatusCode.BadRequest);
            }

            if (responseType == GetUpdatesResponseType)
            {
                try
                {
                    var updatesResponse = MaxJsonSerializer.Deserialize<GetUpdatesResponse>(responseBody);
                    if (updatesResponse?.Updates != null)
                    {
                        return (T)(object)updatesResponse;
                    }
                }
                catch (JsonException)
                {
                    // Response<T> format doesn't match, try wrapped deserialization
                }
            }

            // Try deserializing as Response<T> first
            try
            {
                var wrappedApiResponse = MaxJsonSerializer.Deserialize<Response<T>>(responseBody);
                if (wrappedApiResponse != null && wrappedApiResponse.Ok && wrappedApiResponse.Result != null)
                {
                    return wrappedApiResponse.Result;
                }
            }
            catch (JsonException)
            {
                // Response<T> format doesn't match, try direct deserialization
            }

            // Try direct deserialization as T (for endpoints like /me, /updates that return data directly)
            try
            {
                var directResponse = MaxJsonSerializer.Deserialize<T>(responseBody);
                if (directResponse != null)
                {
                    return directResponse;
                }
            }
            catch (JsonException ex)
            {
                throw new MaxApiException(
                    $"API request failed. The response could not be deserialized as Response<T> or T. Response body: {responseBody}",
                    null,
                    HttpStatusCode.BadRequest,
                    ex);
            }

            throw new MaxApiException(
                "API request failed. The response could not be deserialized as Response<T> or T.",
                null,
                HttpStatusCode.BadRequest);
        }

        // For POST/PUT/PATCH: use SendAsyncRaw to get response body once, then try both strategies (same as GET).
        // This avoids duplicate HTTP calls that could cause duplicate actions.
        var postResponseBody = await HttpClient.SendAsyncRaw(request, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(postResponseBody))
        {
            throw new MaxApiException(
                "API request returned empty response body.",
                null,
                HttpStatusCode.BadRequest);
        }

        // Try deserializing as Response<T> first
        try
        {
            var wrappedApiResponse = MaxJsonSerializer.Deserialize<Response<T>>(postResponseBody);
            if (wrappedApiResponse != null && wrappedApiResponse.Ok && wrappedApiResponse.Result != null)
            {
                return wrappedApiResponse.Result;
            }
        }
        catch (JsonException)
        {
            // Response<T> format doesn't match, try direct deserialization
        }

        // Try direct deserialization as T (for endpoints that return data directly without Response<T> wrapper)
        try
        {
            var directResponse = MaxJsonSerializer.Deserialize<T>(postResponseBody);
            if (directResponse != null)
            {
                return directResponse;
            }
        }
        catch (JsonException ex)
        {
            throw new MaxApiException(
                $"API request failed. The response could not be deserialized as Response<T> or T. Response body: {postResponseBody}",
                null,
                HttpStatusCode.BadRequest,
                ex);
        }

        throw new MaxApiException(
            "API request failed. The response could not be deserialized as Response<T> or T.",
            null,
            HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Validates that a user ID is not negative.
    /// Note: userId = 0 is allowed in some scenarios in Max API.
    /// </summary>
    /// <param name="userId">The user ID to validate.</param>
    /// <param name="paramName">The name of the parameter (default: "userId").</param>
    /// <exception cref="ArgumentException">Thrown when userId is negative.</exception>
    protected static void ValidateUserId(long userId, string paramName = "userId")
    {
        if (userId < 0)
        {
            throw new ArgumentException("User ID cannot be negative.", paramName);
        }
    }

    /// <summary>
    /// Validates that a user ID is greater than zero (strict validation).
    /// Use this for operations that require a specific user (e.g., removing admin/member).
    /// </summary>
    /// <param name="userId">The user ID to validate.</param>
    /// <param name="paramName">The name of the parameter (default: "userId").</param>
    /// <exception cref="ArgumentException">Thrown when userId is less than or equal to zero.</exception>
    protected static void ValidateUserIdStrict(long userId, string paramName = "userId")
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than zero.", paramName);
        }
    }

    /// <summary>
    /// Validates that a string parameter is not null or empty.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="ArgumentException">Thrown when value is null or empty.</exception>
    protected static void ValidateNotEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        }
    }
}

