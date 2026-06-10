using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Max.Bot.Types;
using Max.Bot.Types.Enums;

namespace Max.Bot.Api;

/// <summary>
/// Interface for file-related API methods.
/// </summary>
public interface IFilesApi
{
    /// <summary>
    /// Uploads a file and returns an upload URL and optional token.
    /// </summary>
    /// <remarks>
    /// <para>Max API restricts supported formats:</para>
    /// <list type="bullet">
    /// <item><description>For <c>type=file</c>, unsupported extensions (e.g. <c>.html</c>) return 'File extension is forbidden'.</description></item>
    /// <item><description>For <c>type=image</c>, supported formats: JPG, JPEG, PNG, GIF, TIFF, BMP, HEIC.</description></item>
    /// <item><description>For <c>type=video</c>, supported formats: MP4, MOV, MKV, WEBM.</description></item>
    /// <item><description>For <c>type=audio</c>, supported formats: MP3, WAV, M4A and others.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="uploadType">The type of file to upload.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the upload response with URL and optional token.</returns>
    /// <exception cref="Max.Bot.Exceptions.MaxApiException">Thrown when the API returns an error response.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxNetworkException">Thrown when a network error occurs.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxUnauthorizedException">Thrown when authentication fails.</exception>
    Task<UploadResponse> UploadFileAsync(UploadType uploadType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads file data to the upload URL using multipart/form-data.
    /// </summary>
    /// <param name="uploadUrl">The upload URL obtained from UploadFileAsync.</param>
    /// <param name="fileStream">The stream containing the file data to upload.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <param name="fileName">The name of the file. Used to determine the MIME type. If the extension is unknown, falls back to "application/octet-stream". Optional.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the upload result with a token.</returns>
    /// <exception cref="ArgumentException">Thrown when uploadUrl is null or empty, or fileStream is not readable.</exception>
    /// <exception cref="ArgumentNullException">Thrown when fileStream is null.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxApiException">Thrown when the upload fails.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxNetworkException">Thrown when a network error occurs.</exception>
    Task<FileUploadResult> UploadFileDataAsync(string uploadUrl, Stream fileStream, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads file data in chunks using resumable upload method.
    /// </summary>
    /// <param name="uploadUrl">The upload URL obtained from UploadFileAsync.</param>
    /// <param name="fileStream">The stream containing the file data to upload.</param>
    /// <param name="chunkSize">The size of each chunk in bytes. Default is 1 MB.</param>
    /// <param name="fileName">The name of the file. Optional.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the upload result with a token.</returns>
    /// <exception cref="ArgumentException">Thrown when uploadUrl is null or empty, fileStream is not readable, or chunkSize is less than or equal to zero.</exception>
    /// <exception cref="ArgumentNullException">Thrown when fileStream is null.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxApiException">Thrown when the upload fails.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxNetworkException">Thrown when a network error occurs.</exception>
    Task<FileUploadResult> UploadFileResumableAsync(string uploadUrl, Stream fileStream, long chunkSize = 1024 * 1024, string? fileName = null, CancellationToken cancellationToken = default);
}
