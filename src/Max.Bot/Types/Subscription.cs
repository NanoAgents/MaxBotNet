using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Max.Bot.Types.Enums;

namespace Max.Bot.Types;

/// <summary>
/// Represents a subscription to updates.
/// </summary>
public class Subscription
{
    /// <summary>
    /// Gets or sets the URL where updates will be sent.
    /// </summary>
    /// <value>The webhook URL.</value>
    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    /// <summary>
    /// Gets or sets the list of update types that this subscription receives.
    /// </summary>
    /// <value>List of update types.</value>
    [JsonPropertyName("update_types")]
    public List<UpdateType>? UpdateTypes { get; set; }

    /// <summary>
    /// Gets or sets the secret that is sent in the X-Max-Bot-Api-Secret header.
    /// </summary>
    /// <value>The secret string, if set.</value>
    [JsonPropertyName("secret")]
    public string? Secret { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the subscription was created.
    /// </summary>
    /// <value>The creation timestamp.</value>
    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(Converters.UnixTimestampJsonConverter))]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the subscription was last updated.
    /// </summary>
    /// <value>The last update timestamp.</value>
    [JsonPropertyName("updated_at")]
    [JsonConverter(typeof(Converters.UnixTimestampJsonConverter))]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the Unix time (milliseconds) when the subscription was created, as returned by MAX API.
    /// </summary>
    [JsonPropertyName("time")]
    public long? Time { get; set; }

    /// <summary>
    /// Gets or sets the API version of the subscription.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}




