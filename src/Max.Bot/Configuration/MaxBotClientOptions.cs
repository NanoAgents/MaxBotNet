namespace Max.Bot.Configuration;

/// <summary>
/// Options for configuring the Max Bot HTTP client.
/// </summary>
public class MaxBotClientOptions
{
    /// <summary>
    /// Gets or sets the base URL of the Max Bot API.
    /// </summary>
    /// <value>The base URL of the API (e.g., "https://api.max.ru/bot").</value>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout for HTTP requests.
    /// </summary>
    /// <value>
    /// The timeout for requests. Default is 100 seconds to accommodate long polling requests
    /// (which can take up to 90 seconds) plus network delay buffer.
    /// </value>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>
    /// Gets or sets the number of retry attempts for failed requests.
    /// </summary>
    /// <value>The number of retry attempts. Default is 3.</value>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay for exponential backoff retry mechanism.
    /// </summary>
    /// <value>The base delay for retry attempts. Default is 1 second.</value>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum delay for exponential backoff retry mechanism.
    /// </summary>
    /// <value>The maximum delay for retry attempts. Default is 30 seconds.</value>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether detailed logging is enabled.
    /// </summary>
    /// <value>True if detailed logging is enabled (logs request/response body); otherwise, false. Default is false.</value>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when BaseUrl is null, empty, not a valid absolute URI, or does not use HTTP or HTTPS scheme.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when Timeout, RetryCount, RetryBaseDelay, or MaxRetryDelay have invalid values.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new ArgumentException("BaseUrl cannot be null or empty.", nameof(BaseUrl));
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || !uri.IsAbsoluteUri || string.IsNullOrEmpty(uri.Scheme))
        {
            throw new ArgumentException("BaseUrl must be a valid absolute URI.", nameof(BaseUrl));
        }

        // * Validate that the URI scheme is HTTP or HTTPS
        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("BaseUrl must use HTTP or HTTPS scheme.", nameof(BaseUrl));
        }

        if (Timeout <= TimeSpan.Zero && Timeout != System.Threading.Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(Timeout), "Timeout must be greater than zero or Timeout.InfiniteTimeSpan.");
        }

        if (RetryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RetryCount), "RetryCount cannot be negative.");
        }

        if (RetryBaseDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(RetryBaseDelay), "RetryBaseDelay must be greater than zero.");
        }

        if (MaxRetryDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxRetryDelay), "MaxRetryDelay must be greater than zero.");
        }

        if (MaxRetryDelay < RetryBaseDelay)
        {
            throw new ArgumentException("MaxRetryDelay must be greater than or equal to RetryBaseDelay.", nameof(MaxRetryDelay));
        }
    }
}

