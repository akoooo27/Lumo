namespace Main.Api.Endpoints.Chats.GetAttachmentUploadUrl;

internal sealed record Request
(
    string ContentType,
    long ContentLength
);