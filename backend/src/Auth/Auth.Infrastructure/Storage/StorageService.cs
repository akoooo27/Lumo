using System.Globalization;

using Amazon.S3;
using Amazon.S3.Model;

using Auth.Application.Abstractions.Storage;

using Microsoft.Extensions.Options;

using SharedKernel.Application.Storage;
using SharedKernel.Infrastructure.Options;

namespace Auth.Infrastructure.Storage;

internal sealed class StorageService(IAmazonS3 s3Client, IOptions<S3Options> s3Options) : IStorageService
{
    private readonly S3Options _s3Options = s3Options.Value;

    public async Task<PresignedUploadUrl> GetPresignedUploadUrlAsync(string fileKey, string contentType, long contentLength, TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(expiration);

        GetPreSignedUrlRequest request = new()
        {
            BucketName = _s3Options.BucketName,
            Key = fileKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = contentType,
            Headers =
            {
                ["Content-Length"] = contentLength.ToString(CultureInfo.InvariantCulture)
            }
        };

        string url = await s3Client.GetPreSignedURLAsync(request);

        PresignedUploadUrl presignedUploadUrl = new(url, expiresAt);

        return presignedUploadUrl;
    }

    public async Task<bool> FileExistsAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            GetObjectMetadataRequest request = new()
            {
                BucketName = _s3Options.BucketName,
                Key = fileKey
            };

            await s3Client.GetObjectMetadataAsync(request, cancellationToken);

            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public Task<bool> IsOwnedByAsync(string fileKey, Guid userId, CancellationToken cancellationToken = default)
    {
        string expectedPrefix = $"{AvatarConstants.AvatarFolder}/{userId:N}/";

        return Task.FromResult(
            !string.IsNullOrWhiteSpace(fileKey) &&
            fileKey.StartsWith(expectedPrefix, StringComparison.Ordinal));
    }

    public async Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ListObjectsV2Request listRequest = new()
        {
            BucketName = _s3Options.BucketName,
            Prefix = prefix
        };

        ListObjectsV2Response listResponse;

        do
        {
            listResponse = await s3Client.ListObjectsV2Async(listRequest, cancellationToken);

            if (listResponse.S3Objects.Count == 0)
                break;

            DeleteObjectsRequest deleteRequest = new()
            {
                BucketName = _s3Options.BucketName,
                Objects = [.. listResponse.S3Objects.Select(obj => new KeyVersion { Key = obj.Key })]
            };

            await s3Client.DeleteObjectsAsync(deleteRequest, cancellationToken);

            listRequest.ContinuationToken = listResponse.NextContinuationToken;

        } while (listResponse.IsTruncated == true);
    }
}