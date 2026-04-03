namespace Main.Application.Abstractions.Storage;

public static class AttachmentConstants
{
    public const string AttachmentFolder = "attachments";

    public const long MaxFileSizeInBytes = 10 * 1024 * 1024;

    public const int PresignedUrlExpirationMinutes = 15;

    public static readonly IReadOnlyList<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "application/pdf"
    ];
}