using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Chats.GetAttachmentUploadUrl;

public sealed record GetAttachmentUploadUrlCommand
(
    string ContentType,
    long ContentLength
) : ICommand<GetAttachmentUploadUrlResponse>;