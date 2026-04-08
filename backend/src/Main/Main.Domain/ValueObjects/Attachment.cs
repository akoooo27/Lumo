using Main.Domain.Enums;

using SharedKernel;

namespace Main.Domain.ValueObjects;

public sealed record Attachment
{
    public const long MaxFileSizeInBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<string, ContentTypeInfo> SupportedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = new ContentTypeInfo(FileCategory.Image, RequiresBinaryDelivery: false),
            ["image/png"] = new ContentTypeInfo(FileCategory.Image, RequiresBinaryDelivery: false),
            ["image/gif"] = new ContentTypeInfo(FileCategory.Image, RequiresBinaryDelivery: false),
            ["image/webp"] = new ContentTypeInfo(FileCategory.Image, RequiresBinaryDelivery: false),
            ["application/pdf"] = new ContentTypeInfo(FileCategory.Document, RequiresBinaryDelivery: true),
        };

    public static IReadOnlyList<string> AllowedContentTypes { get; } =
        SupportedContentTypes.Keys.ToList().AsReadOnly();

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

        if (!SupportedContentTypes.ContainsKey(contentType))
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

    public static bool IsSupported(string contentType) =>
        SupportedContentTypes.ContainsKey(contentType);

    public static bool TryGetContentTypeInfo(string contentType, out ContentTypeInfo info) =>
        SupportedContentTypes.TryGetValue(contentType, out info);

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

public readonly record struct ContentTypeInfo(FileCategory Category, bool RequiresBinaryDelivery);