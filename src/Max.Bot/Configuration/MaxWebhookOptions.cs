using System;

namespace Max.Bot.Configuration;

/// <summary>
/// Describes webhook subscription preferences following the guidance from https://dev.max.ru/docs-api/methods/POST/subscriptions.
/// </summary>
public sealed class MaxWebhookOptions
{
    /// <summary>
    /// Gets or sets the public HTTPS endpoint that MAX should call (maps to subscription <c>url</c> payload).
    /// Used by <see cref="MaxClient.ConfigureWebhookAsync"/> and <see cref="MaxClient.DeleteWebhookAsync"/> when no URL is passed.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether HTTPS is mandatory for <see cref="Endpoint"/>.
    /// </summary>
    public bool EnforceHttps { get; set; } = true;

    /// <summary>
    /// Gets or sets the shared secret (5-256 characters: A-Z, a-z, 0-9, <c>_</c> and <c>-</c>) passed to MAX as the subscription
    /// <c>secret</c>. MAX sends it back in the <see cref="SignatureHeaderName"/> header of every webhook request,
    /// and incoming requests without a matching header are rejected.
    /// </summary>
    public string? SecretToken { get; set; }

    /// <summary>
    /// The header MAX uses to send the subscription secret with every webhook request.
    /// </summary>
    public const string DefaultSignatureHeaderName = "X-Max-Bot-Api-Secret";

    /// <summary>
    /// Gets or sets the header name storing the webhook secret token. Defaults to <c>X-Max-Bot-Api-Secret</c>.
    /// </summary>
    public string SignatureHeaderName { get; set; } = DefaultSignatureHeaderName;

    /// <summary>
    /// Gets or sets the maximum accepted request body size for webhook payloads (in KiB).
    /// </summary>
    public int MaxBodySizeKilobytes { get; set; } = 512;

    /// <summary>
    /// Gets or sets the per-request execution timeout applied while dispatching updates.
    /// </summary>
    public TimeSpan HandlerTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether every subscription change must be confirmed with MAX via POST/DELETE /subscriptions.
    /// </summary>
    public bool RequireSubscriptionConfirmation { get; set; } = true;

    /// <summary>
    /// Validates option values.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (!string.IsNullOrWhiteSpace(Endpoint))
        {
            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException("Endpoint must be a valid absolute URI.", nameof(Endpoint));
            }

            if (EnforceHttps && !Uri.UriSchemeHttps.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Endpoint must use HTTPS when EnforceHttps is enabled.", nameof(Endpoint));
            }
        }

        if (MaxBodySizeKilobytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxBodySizeKilobytes), MaxBodySizeKilobytes, "MaxBodySizeKilobytes must be greater than zero.");
        }

        if (HandlerTimeout <= TimeSpan.Zero && HandlerTimeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(HandlerTimeout), HandlerTimeout, "HandlerTimeout must be greater than zero or Timeout.InfiniteTimeSpan.");
        }

        if (string.IsNullOrWhiteSpace(SignatureHeaderName))
        {
            throw new ArgumentException("SignatureHeaderName cannot be null or whitespace.", nameof(SignatureHeaderName));
        }
    }
}






