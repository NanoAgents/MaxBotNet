using System.Text.Json;
using System.Text.Json.Serialization;

namespace Max.Bot.Types.Converters;

/// <summary>
/// Serializes an enum as the snake_case string used by MAX Bot API (for example, <c>TypingOn</c> → <c>"typing_on"</c>)
/// and reads both snake_case and PascalCase names case-insensitively.
/// </summary>
/// <remarks>
/// Every enum of the library is marked with this converter, so enum values are (de)serialized correctly not only with
/// <see cref="Networking.MaxJsonSerializer.Options"/> but also with default options, for example
/// <c>JsonSerializer.Deserialize&lt;MessageLinkType&gt;("\"reply\"")</c>. An enum converter registered in
/// <see cref="JsonSerializerOptions.Converters"/> takes precedence over this attribute.
/// An unknown string value throws <see cref="JsonException"/>; library response models use
/// <see cref="LenientNullableEnumConverter{TEnum}"/> where MAX may add new values.
/// </remarks>
/// <typeparam name="TEnum">The enum type.</typeparam>
public sealed class JsonSnakeCaseEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSnakeCaseEnumConverter{TEnum}"/> class.
    /// </summary>
    public JsonSnakeCaseEnumConverter()
        : base(JsonNamingPolicy.SnakeCaseLower)
    {
    }
}
