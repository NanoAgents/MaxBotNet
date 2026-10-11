using System.Net;

namespace Max.Bot.Exceptions;

/// <summary>
/// Exception thrown when MAX API rejects a message because an attachment uploaded moments ago
/// has not been processed yet (error code <c>attachment.not.ready</c>).
/// </summary>
/// <remarks>
/// The request can be retried after a short delay. <c>SendMessageWithAttachmentAsync</c>,
/// <c>SendMessageWithAttachmentsAsync</c> and <c>EditMessageAsync</c> already retry this error automatically.
/// </remarks>
public class MaxAttachmentNotReadyException : MaxApiException
{
    /// <summary>
    /// The error code returned by MAX API when an uploaded attachment is not processed yet.
    /// </summary>
    public const string Code = "attachment.not.ready";

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxAttachmentNotReadyException"/> class.
    /// </summary>
    public MaxAttachmentNotReadyException()
        : base("Attachment is not ready yet.", Code, System.Net.HttpStatusCode.BadRequest)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxAttachmentNotReadyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MaxAttachmentNotReadyException(string message)
        : base(message, Code, System.Net.HttpStatusCode.BadRequest)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxAttachmentNotReadyException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MaxAttachmentNotReadyException(string message, Exception innerException)
        : base(message, Code, System.Net.HttpStatusCode.BadRequest, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxAttachmentNotReadyException"/> class with a specified error message, error code, and HTTP status code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code from the API response.</param>
    /// <param name="httpStatusCode">The HTTP status code from the API response.</param>
    public MaxAttachmentNotReadyException(string message, string? errorCode, HttpStatusCode? httpStatusCode)
        : base(message, errorCode ?? Code, httpStatusCode)
    {
    }

    /// <summary>
    /// Determines whether an API error code means that an uploaded attachment is not processed yet.
    /// </summary>
    /// <param name="errorCode">The error code from the API response.</param>
    /// <returns><c>true</c> when the error code is <c>attachment.not.ready</c>; otherwise, <c>false</c>.</returns>
    public static bool IsAttachmentNotReadyCode(string? errorCode)
    {
        return string.Equals(errorCode, Code, StringComparison.OrdinalIgnoreCase);
    }
}
