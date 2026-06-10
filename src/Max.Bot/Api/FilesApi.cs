using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Max.Bot.Configuration;
using Max.Bot.Exceptions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Max.Bot.Types.Enums;

namespace Max.Bot.Api;

/// <summary>
/// Implementation of file-related API methods.
/// </summary>
internal class FilesApi : BaseApi, IFilesApi
{
    public FilesApi(IMaxHttpClient httpClient, MaxBotOptions options)
        : base(httpClient, options)
    {
    }

    public async Task<UploadResponse> UploadFileAsync(UploadType uploadType, CancellationToken cancellationToken = default)
    {
        var typeString = uploadType switch
        {
            UploadType.Image => "image",
            UploadType.Video => "video",
            UploadType.Audio => "audio",
            UploadType.File => "file",
            _ => throw new ArgumentException($"Unknown upload type: {uploadType}", nameof(uploadType))
        };

        var queryParams = new Dictionary<string, string?> { { "type", typeString } };
        var request = CreateRequest(HttpMethod.Post, "/uploads", null, queryParams);
        return await ExecuteRequestAsync<UploadResponse>(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FileUploadResult> UploadFileDataAsync(string uploadUrl, Stream fileStream, string? fileName = null, CancellationToken cancellationToken = default)
    {
        ValidateNotEmpty(uploadUrl, nameof(uploadUrl));
        ArgumentNullException.ThrowIfNull(fileStream);

        // Factory to create content for retries
        HttpContent CreateContent()
        {
            var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(new NonDisposingStreamWrapper(fileStream));
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(MimeTypes.FromFileName(fileName) ?? "application/octet-stream");
            content.Add(streamContent, "data", fileName ?? "file");
            return content;
        }

        var responseBody = await HttpClient.SendAsyncRaw(uploadUrl, CreateContent, cancellationToken).ConfigureAwait(false);
        return ParseUploadResponse(responseBody);
    }

    public async Task<FileUploadResult> UploadFileResumableAsync(string uploadUrl, Stream fileStream, long chunkSize = 1024 * 1024, string? fileName = null, CancellationToken cancellationToken = default)
    {
        ValidateNotEmpty(uploadUrl, nameof(uploadUrl));
        ArgumentNullException.ThrowIfNull(fileStream);
        if (chunkSize <= 0) throw new ArgumentException("Chunk size must be greater than zero.", nameof(chunkSize));

        var buffer = new byte[chunkSize];
        long totalBytesRead = 0;
        long fileLength = fileStream.CanSeek ? fileStream.Length : -1;

        while (true)
        {
            var bytesRead = await fileStream.ReadAsync(buffer, 0, (int)chunkSize, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0) break;

            var currentOffset = totalBytesRead;
            var currentBytesRead = bytesRead;

            // Клонируем данные чанка, чтобы избежать перезаписи буфера при следующей итерации
            // (HttpClient отправляет данные асинхронно и может читать буфер, когда мы уже пишем в него следующий чанк)
            var chunkData = new byte[currentBytesRead];
            Buffer.BlockCopy(buffer, 0, chunkData, 0, currentBytesRead);

            HttpContent CreateChunkContent()
            {
                var content = new ByteArrayContent(chunkData);
                content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypes.FromFileName(fileName) ?? "application/octet-stream");
                // Always send Content-Range. If length unknown, use '*'
                content.Headers.ContentRange = fileLength > 0
                    ? new ContentRangeHeaderValue(currentOffset, currentOffset + currentBytesRead - 1, fileLength)
                    : new ContentRangeHeaderValue(currentOffset, currentOffset + currentBytesRead - 1);
                return content;
            }

            var responseBody = await HttpClient.SendAsyncRaw(uploadUrl, CreateChunkContent, cancellationToken).ConfigureAwait(false);

            totalBytesRead += bytesRead;

            // Check if this chunk's response already contains the token/result
            var result = ParseUploadResponse(responseBody);
            if (result.Token != null)
            {
                return result;
            }

            if (fileLength > 0 && totalBytesRead >= fileLength) break;
        }

        // Finalize: If we reached here without a token, try a final GET
        var finalResponseBody = await HttpClient.SendAsyncRaw(uploadUrl, null, cancellationToken, HttpMethod.Get).ConfigureAwait(false);
        return ParseUploadResponse(finalResponseBody);
    }

    private static FileUploadResult ParseUploadResponse(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return new FileUploadResult();
        try
        {
            return JsonSerializer.Deserialize<FileUploadResult>(responseBody, MaxJsonSerializer.Options) ?? new FileUploadResult();
        }
        catch (JsonException)
        {
            // Fallback for raw token strings
            return new FileUploadResult { Token = responseBody };
        }
    }

    private sealed class NonDisposingStreamWrapper : Stream
    {
        private readonly Stream _inner;
        public NonDisposingStreamWrapper(Stream inner) => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override bool CanTimeout => _inner.CanTimeout;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override int ReadTimeout { get => _inner.ReadTimeout; set => _inner.ReadTimeout = value; }
        public override int WriteTimeout { get => _inner.WriteTimeout; set => _inner.WriteTimeout = value; }

        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => _inner.Read(buffer);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);
        public override int ReadByte() => _inner.ReadByte();

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => _inner.Write(buffer);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.WriteAsync(buffer, offset, count, cancellationToken);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _inner.WriteAsync(buffer, cancellationToken);
        public override void WriteByte(byte value) => _inner.WriteByte(value);

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => _inner.CopyToAsync(destination, bufferSize, cancellationToken);

        protected override void Dispose(bool disposing) { base.Dispose(disposing); }
        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
