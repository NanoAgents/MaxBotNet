// 📁 ChatType.cs - Chat type enum for API JSON
// 🎯 Core function: Maps Max API chat type strings to enum values.
// 🔗 Key dependencies: System.Text.Json enum converter attributes.
// 💡 Usage: Used by Chat and other chat-related payloads.

using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;

namespace Max.Bot.Types.Enums;

/// <summary>
/// Represents the type of a chat.
/// Maps to the official Max API chat type values.
/// </summary>
[JsonConverter(typeof(JsonSnakeCaseEnumConverter<ChatType>))]
public enum ChatType
{
    /// <summary>
    /// Group chat (type: "chat").
    /// </summary>
    Chat,

    /// <summary>
    /// Private dialog with a user (type: "dialog").
    /// </summary>
    Dialog,

    /// <summary>
    /// Channel (type: "channel").
    /// </summary>
    Channel,
}
