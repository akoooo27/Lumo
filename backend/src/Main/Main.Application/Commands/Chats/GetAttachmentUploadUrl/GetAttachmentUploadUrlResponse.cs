namespace Main.Application.Commands.Chats.GetAttachmentUploadUrl;

public sealed record GetAttachmentUploadUrlResponse
(
    string UploadUrl,
    string FileKey,
    DateTimeOffset ExpiresAt
);