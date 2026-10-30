using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Max.Bot.Types.Enums;

namespace Max.Bot.Types.Requests;

/// <summary>
/// Represents a request to set a webhook.
/// </summary>
public class SetWebhookRequest
{
    /// <summary>
    /// Gets or sets the URL where updates will be sent.
    /// </summary>
    /// <value>The webhook URL (must be HTTPS).</value>
    [Required]
    [Url(ErrorMessage = "URL must be a valid HTTPS URL.")]
    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    /// <summary>
    /// Gets or sets the list of update types to receive.
    /// </summary>
    /// <value>List of update types (e.g., <see cref="UpdateType.MessageCreated"/>, <see cref="UpdateType.BotStarted"/>).</value>
    [JsonPropertyName("update_types")]
    public List<UpdateType>? UpdateTypes { get; set; }

    /// <summary>
    /// Gets or sets the secret that will be sent in the X-Max-Bot-Api-Secret header.
    /// </summary>
    /// <value>The secret string (5-256 characters, A-Z, a-z, 0-9, hyphen, underscore).</value>
    [JsonPropertyName("secret")]
    public string? Secret { get; set; }

    /// <summary>
    /// Gets or sets the X.509 certificate in PEM format. Needed only if the webhook endpoint uses a self-signed certificate.
    /// </summary>
    [JsonPropertyName("self_signed_cert")]
    public string? SelfSignedCert { get; set; }

    /// <summary>
    /// Gets or sets the Bot API version that defines the model representation of webhook updates (for example, <c>"0.0.30"</c>).
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}




