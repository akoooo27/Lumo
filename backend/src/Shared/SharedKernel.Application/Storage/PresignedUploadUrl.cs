namespace SharedKernel.Application.Storage;

public sealed record PresignedUploadUrl
(
    string Url,
    DateTimeOffset ExpiresAt
);