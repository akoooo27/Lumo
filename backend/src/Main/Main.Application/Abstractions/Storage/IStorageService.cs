using SharedKernel.Application.Storage;

namespace Main.Application.Abstractions.Storage;

public interface IStorageService
{
    Task<PresignedUploadUrl> GetPresignedUploadUrlAsync(string fileKey, string contentType, long contentLength,
        TimeSpan expiration, CancellationToken cancellationToken = default);

    Task<string> GetPresignedReadUrlAsync(string fileKey, TimeSpan expiration,
        CancellationToken cancellationToken = default);

    Task<bool> FileExistsAsync(string fileKey, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string fileKey, CancellationToken cancellationToken = default);

    Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    Task<byte[]> DownloadFileAsync(string fileKey, CancellationToken cancellationToken = default);
}