using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Max.Bot.Types.Converters;

/// <summary>
/// Reads a nullable enum from a snake_case string and returns <c>null</c> for values unknown to this library version,
/// so that new values added by MAX API do not break deserialization of the whole response.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
public sealed class LenientNullableEnumConverter<TEnum> : JsonConverter<TEnum?>
    where TEnum : struct, Enum
{
    /// <inheritdoc />
    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return LenientEnumParser.TryParse<TEnum>(reader.GetString(), out var value) ? value : null;
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number) && Enum.IsDefined(typeof(TEnum), number))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), number);
        }

        reader.Skip();
        return null;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(LenientEnumParser.ToSnakeCase(value.Value.ToString()));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>
/// Reads an array of enums from snake_case strings, skipping values unknown to this library version.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
public sealed class LenientEnumArrayConverter<TEnum> : JsonConverter<TEnum[]?>
    where TEnum : struct, Enum
{
    /// <inheritdoc />
    public override TEnum[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return null;
        }

        var values = new List<TEnum>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String && LenientEnumParser.TryParse<TEnum>(reader.GetString(), out var value))
            {
                values.Add(value);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return values.ToArray();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum[]? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(LenientEnumParser.ToSnakeCase(item.ToString()));
        }

        writer.WriteEndArray();
    }
}

internal static class LenientEnumParser
{
    public static bool TryParse<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Replace("_", string.Empty, StringComparison.Ordinal).Trim();
        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (string.Equals(candidate.ToString(), normalized, StringComparison.OrdinalIgnoreCase))
            {
                result = candidate;
                return true;
            }
        }

        return false;
    }

    public static string ToSnakeCase(string value) => JsonNamingPolicy.SnakeCaseLower.ConvertName(value);
}
