using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Max.Bot.Networking;

namespace Max.Bot.Types;

/// <summary>
/// Represents the response from the GetUpdates endpoint.
/// </summary>
public class GetUpdatesResponse
{
    /// <summary>
    /// Gets or sets the list of updates.
    /// </summary>
    /// <value>The list of updates.</value>
    [JsonPropertyName("updates")]
    public Update[] Updates { get; set; } = default!;

    /// <summary>
    /// Gets or sets the marker for the next page of updates.
    /// </summary>
    /// <value>The marker for the next page, or null if no more updates.</value>
    [JsonPropertyName("marker")]
    public long? Marker { get; set; }

    /// <summary>
    /// Parses a <c>GET /updates</c> response update by update, so that one update the library cannot deserialize
    /// does not block the whole batch (the marker would otherwise never advance and polling would loop forever).
    /// </summary>
    /// <param name="json">The raw response body.</param>
    /// <param name="onSkipped">Called for every update that could not be deserialized.</param>
    /// <returns>The parsed response.</returns>
    /// <exception cref="JsonException">Thrown when the body is not a JSON object.</exception>
    internal static GetUpdatesResponse ParseTolerant(string json, Action<JsonException, string>? onSkipped = null)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("GET /updates response must be a JSON object.");
        }

        // Legacy {"ok": true, "result": {"updates": [...], "marker": ...}} envelope.
        if (!root.TryGetProperty("updates", out _) &&
            root.TryGetProperty("result", out var wrapped) && wrapped.ValueKind == JsonValueKind.Object)
        {
            root = wrapped;
        }

        var response = new GetUpdatesResponse { Updates = Array.Empty<Update>() };

        if (root.TryGetProperty("marker", out var markerElement) && markerElement.ValueKind == JsonValueKind.Number && markerElement.TryGetInt64(out var marker))
        {
            response.Marker = marker;
        }

        if (root.TryGetProperty("updates", out var updatesElement) && updatesElement.ValueKind == JsonValueKind.Array)
        {
            var updates = new List<Update>(updatesElement.GetArrayLength());
            foreach (var element in updatesElement.EnumerateArray())
            {
                try
                {
                    var update = element.Deserialize<Update>(MaxJsonSerializer.Options);
                    if (update != null)
                    {
                        updates.Add(update);
                    }
                }
                catch (JsonException ex)
                {
                    onSkipped?.Invoke(ex, element.GetRawText());
                }
                catch (InvalidOperationException ex)
                {
                    onSkipped?.Invoke(new JsonException(ex.Message, ex), element.GetRawText());
                }
            }

            response.Updates = updates.ToArray();
        }

        return response;
    }
}






