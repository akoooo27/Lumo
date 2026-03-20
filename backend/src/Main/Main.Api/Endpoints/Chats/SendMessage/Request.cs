namespace Main.Api.Endpoints.Chats.SendMessage;

internal sealed record Request
(
    string ChatId,
    string Message,
    bool WebSearchEnabled = false,
    AttachmentRequest? Attachment = null
);

internal sealed record AttachmentRequest
(
    string FileKey,
    string ContentType,
    long FileSizeInBytes
);