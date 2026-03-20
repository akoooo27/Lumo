using SharedKernel.Application.Storage;

namespace Auth.Application.Abstractions.Storage;

public interface IStorageService
{
    Task<PresignedUploadUrl> GetPresignedUploadUrlAsync(string fileKey, string contentType, long contentLength,
        TimeSpan expiration, CancellationToken cancellationToken = default);

    Task<bool> FileExistsAsync(string fileKey, CancellationToken cancellationToken = default);

    Task<bool> IsOwnedByAsync(string fileKey, Guid userId, CancellationToken cancellationToken = default);
}