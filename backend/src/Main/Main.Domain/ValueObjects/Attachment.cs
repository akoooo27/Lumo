using SharedKernel;

namespace Main.Domain.ValueObjects;

public sealed record Attachment
{
    private const long MaxFileSizeInBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    public string FileKey { get; }

    public string ContentType { get; }

    public long FileSizeInBytes { get; }

    private Attachment
    (
        string fileKey,
        string contentType,
        long fileSizeInBytes
    )
    {
        FileKey = fileKey;
        ContentType = contentType;
        FileSizeInBytes = fileSizeInBytes;
    }

    public static Outcome<Attachment> Create(string fileKey, string contentType, long fileSizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            return Faults.FileKeyRequired;

        if (!AllowedContentTypes.Contains(contentType))
            return Faults.UnsupportedContentType;

        if (fileSizeInBytes is <= 0 or > MaxFileSizeInBytes)
            return Faults.InvalidFileSize;

        Attachment attachment = new
        (
            fileKey: fileKey,
            contentType: contentType,
            fileSizeInBytes: fileSizeInBytes
        );

        return attachment;
    }

    private static class Faults
    {
        public static readonly Fault FileKeyRequired = Fault.Validation
        (
            title: "Attachment.FileKeyRequired",
            detail: "File key is required."
        );

        public static readonly Fault UnsupportedContentType = Fault.Validation
        (
            title: "Attachment.UnsupportedContentType",
            detail: $"Unsupported content type. Allowed types: {string.Join(", ", AllowedContentTypes)}."
        );

        public static readonly Fault InvalidFileSize = Fault.Validation
        (
            title: "Attachment.InvalidFileSize",
            detail: $"Invalid file size. File size must be greater than 0 and less than or equal to {MaxFileSizeInBytes} bytes."
        );
    }
}