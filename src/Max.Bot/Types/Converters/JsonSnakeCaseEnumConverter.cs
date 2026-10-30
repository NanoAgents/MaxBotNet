using System.Text.Json;
using System.Text.Json.Serialization;

namespace Max.Bot.Types.Converters;

/// <summary>
/// Serializes an enum as the snake_case string used by MAX Bot API (for example, <c>TypingOn</c> → <c>"typing_on"</c>)
/// and reads both snake_case and PascalCase names case-insensitively.
/// </summary>
/// <remarks>
/// Every enum of the library is marked with this converter, so enum values are (de)serialized correctly with any
/// <see cref="JsonSerializerOptions"/> — including <c>JsonSerializer.Deserialize&lt;MessageLinkType&gt;("\"reply\"")</c>
/// with default options — and not only with <see cref="Networking.MaxJsonSerializer.Options"/>.
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
