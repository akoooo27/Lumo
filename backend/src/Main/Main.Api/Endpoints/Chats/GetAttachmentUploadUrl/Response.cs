namespace Main.Api.Endpoints.Chats.GetAttachmentUploadUrl;

internal sealed record Response
(
    string UploadUrl,
    string FileKey,
    DateTimeOffset ExpiresAt
);