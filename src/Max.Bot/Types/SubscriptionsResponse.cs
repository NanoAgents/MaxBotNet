using System.Text.Json.Serialization;

namespace Max.Bot.Types;

/// <summary>
/// Represents the response from the GetSubscriptions endpoint.
/// </summary>
public class SubscriptionsResponse
{
    /// <summary>
    /// Gets or sets the list of subscriptions.
    /// </summary>
    /// <value>The list of subscriptions.</value>
    [JsonPropertyName("subscriptions")]
    public Subscription[] Subscriptions { get; set; } = Array.Empty<Subscription>();
}






