using System.Text.Json.Serialization;
using Max.Bot.Types.Converters;

namespace Max.Bot.Types.Enums;

/// <summary>
/// Represents actions that can be sent by the bot in a chat.
/// </summary>
[JsonConverter(typeof(JsonSnakeCaseEnumConverter<ChatAction>))]
public enum ChatAction
{
    /// <summary>
    /// Bot is typing a message.
    /// Serialized as <c>"typing_on"</c>.
    /// </summary>
    TypingOn,

    /// <summary>
    /// Bot is sending a photo.
    /// Serialized as <c>"sending_photo"</c>.
    /// </summary>
    SendingPhoto,

    /// <summary>
    /// Bot is sending a video.
    /// Serialized as <c>"sending_video"</c>.
    /// </summary>
    SendingVideo,

    /// <summary>
    /// Bot is sending an audio file.
    /// Serialized as <c>"sending_audio"</c>.
    /// </summary>
    SendingAudio,

    /// <summary>
    /// Bot is sending a file.
    /// Serialized as <c>"sending_file"</c>.
    /// </summary>
    SendingFile,

    /// <summary>
    /// Bot marks messages as seen.
    /// Serialized as <c>"mark_seen"</c>.
    /// </summary>
    MarkSeen
}
