using System.Text.Json;
using System.Text.Json.Serialization;

namespace Max.Bot.Types.Converters;

/// <summary>
/// Reads a JSON <c>null</c> as <c>default(T)</c> for a non-nullable value-type property that MAX API documents as required,
/// so an unexpected <c>null</c> does not break deserialization of the whole response or update.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class NullAsDefaultConverter<T> : JsonConverter<T>
    where T : struct
{
    /// <inheritdoc />
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.Null
            ? default
            : JsonSerializer.Deserialize<T>(ref reader, options);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

/// <summary>
/// Reads a JSON <c>null</c> as <see cref="string.Empty"/> for a non-nullable string property that MAX API documents as required.
/// </summary>
public sealed class NullAsEmptyStringConverter : JsonConverter<string>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.Null
            ? string.Empty
            : reader.GetString() ?? string.Empty;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value ?? string.Empty);
    }
}

/// <summary>
/// Reads a JSON <c>null</c> as a new empty <typeparamref name="T"/> for a non-nullable object property that MAX API documents as required.
/// </summary>
/// <typeparam name="T">The object type.</typeparam>
public sealed class NullAsNewConverter<T> : JsonConverter<T>
    where T : class, new()
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.Null
            ? new T()
            : JsonSerializer.Deserialize<T>(ref reader, options) ?? new T();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value ?? new T(), options);
    }
}
