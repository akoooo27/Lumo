namespace Main.Application.Abstractions.Storage;

public sealed record AttachmentDto
(
    string FileKey,
    string ContentType,
    long FileSizeInBytes
);