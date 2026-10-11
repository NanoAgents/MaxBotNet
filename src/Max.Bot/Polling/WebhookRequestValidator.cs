using System.Security.Cryptography;
using System.Text;
using Max.Bot.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Max.Bot.Polling;

/// <summary>
/// Shared validation of incoming MAX webhook requests (payload size and secret header).
/// </summary>
internal static class WebhookRequestValidator
{
    public static long GetMaxBodyBytes(MaxBotOptions options) => options.Webhook.MaxBodySizeKilobytes * 1024L;

    public static bool ValidatePayloadSize(HttpRequest request, MaxBotOptions options, ILogger? logger)
    {
        if (request.ContentLength is null)
        {
            return true;
        }

        var maxBytes = GetMaxBodyBytes(options);
        if (request.ContentLength > maxBytes)
        {
            logger?.LogWarning("Webhook payload rejected: {Length} bytes exceeds limit {Limit} bytes.", request.ContentLength, maxBytes);
            return false;
        }

        return true;
    }

    public static bool ValidateSignature(HttpRequest request, MaxBotOptions options, ILogger? logger)
    {
        var secret = options.Webhook.SecretToken;
        if (string.IsNullOrWhiteSpace(secret))
        {
            return true;
        }

        var headerName = options.Webhook.SignatureHeaderName;
        if (!request.Headers.TryGetValue(headerName, out var headerValues))
        {
            logger?.LogWarning("Webhook payload rejected: missing header {HeaderName}.", headerName);
            return false;
        }

        if (!SecureEquals(headerValues.ToString(), secret))
        {
            logger?.LogWarning("Webhook payload rejected: signature validation failed.");
            return false;
        }

        return true;
    }

    private static bool SecureEquals(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        if (providedBytes.Length != expectedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
