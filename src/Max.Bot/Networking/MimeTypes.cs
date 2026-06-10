using System.Collections.Generic;

namespace Max.Bot.Networking;

/// <summary>
/// Maps file extensions to MIME types for upload Content-Type detection.
/// </summary>
internal static class MimeTypes
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg",  "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png",  "image/png" },
        { ".gif",  "image/gif" },
        { ".webp", "image/webp" },
        { ".bmp",  "image/bmp" },
        { ".tiff", "image/tiff" },
        { ".tif",  "image/tiff" },
        { ".heic", "image/heic" },
        { ".mp4",       "video/mp4" },
        { ".mov",       "video/quicktime" },
        { ".mkv",       "video/x-matroska" },
        { ".webm",      "video/webm" },
        { ".mp3",  "audio/mpeg" },
        { ".wav",  "audio/wav" },
        { ".m4a",  "audio/mp4" },
        { ".ogg",  "audio/ogg" },
        { ".opus", "audio/opus" },
    };

    internal static string? FromFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;
        var ext = fileName.LastIndexOf('.') is >= 0 and var idx
            ? fileName[idx..]
            : fileName;
        return Map.TryGetValue(ext, out var mime) ? mime : null;
    }
}
