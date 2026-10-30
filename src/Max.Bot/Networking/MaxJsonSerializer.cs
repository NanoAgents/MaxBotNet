using System.Text.Json;
using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;

namespace Max.Bot.Networking;

/// <summary>
/// Static class providing JSON serialization and deserialization utilities.
/// </summary>
public static class MaxJsonSerializer
{
    /// <summary>
    /// Gets the JsonSerializerOptions used for serialization and deserialization.
    /// </summary>
    /// <value>The JSON serializer options configured for camelCase property naming, null handling, and custom converters (enum and Unix timestamp).</value>
    public static JsonSerializerOptions Options { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            // * UpdateTypeJsonConverter must precede the generic enum converter: the first matching converter wins,
            // * and it maps update types unknown to this library version to UpdateType.Unknown instead of throwing.
            new Types.Converters.UpdateTypeJsonConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower),
            new UnixTimestampJsonConverter(),
            new Types.Converters.AttachmentJsonConverter()
        }
    };

    /// <summary>
    /// Serializes the specified value to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>A JSON string representation of the value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static string Serialize<T>(T value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return JsonSerializer.Serialize(value, Options);
    }

    /// <summary>
    /// Deserializes the JSON string to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object of type T.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json is null or empty.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to the specified type.</exception>
    public static T Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentNullException(nameof(json));
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(json, Options);

            // For nullable reference types (T?), null is valid. For non-nullable types, throw.
            if (result == null && (!typeof(T).IsGenericType ||
                (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() != typeof(Nullable<>))))
            {
                throw new JsonException("Deserialization returned null for non-nullable type.");
            }

            return result!;
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Error deserializing JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes the JSON from the specified stream to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="stream">The stream containing JSON data.</param>
    /// <returns>The deserialized object of type T.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to the specified type.</exception>
    public static T Deserialize<T>(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(stream, Options);

            // For nullable reference types (T?), null is valid. For non-nullable types, throw.
            if (result == null && (!typeof(T).IsGenericType ||
                (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() != typeof(Nullable<>))))
            {
                throw new JsonException("Deserialization returned null for non-nullable type.");
            }

            return result!;
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Error deserializing JSON from stream: {ex.Message}", ex);
        }
    }
}

