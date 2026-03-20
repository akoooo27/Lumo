namespace Main.Api.Endpoints.Chats.Start;

internal sealed record Request
(
    string Message,
    string? ModelId = null,
    bool WebSearchEnabled = false,
    AttachmentRequest? Attachment = null
);

internal sealed record AttachmentRequest
(
    string FileKey,
    string ContentType,
    long FileSizeInBytes
);