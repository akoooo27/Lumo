using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Storage;
using Main.Application.Faults;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;
using SharedKernel.Application.Storage;

namespace Main.Application.Commands.Chats.GetAttachmentUploadUrl;

internal sealed class GetAttachmentUploadUrlHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IStorageService storageService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<GetAttachmentUploadUrlCommand, GetAttachmentUploadUrlResponse>
{
    public async ValueTask<Outcome<GetAttachmentUploadUrlResponse>> Handle(GetAttachmentUploadUrlCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        bool userExists = await dbContext.Users
            .AnyAsync(u => u.UserId == userId, cancellationToken);

        if (!userExists)
            return UserOperationFaults.NotFound;

        string fileKey =
            $"{AttachmentConstants.AttachmentFolder}/{userId:N}/{Guid.NewGuid():N}/{dateTimeProvider.UtcNow.Ticks}";

        PresignedUploadUrl presignedUploadUrl = await storageService.GetPresignedUploadUrlAsync
        (
            fileKey: fileKey,
            contentType: request.ContentType,
            contentLength: request.ContentLength,
            expiration: TimeSpan.FromMinutes(AttachmentConstants.PresignedUrlExpirationMinutes),
            cancellationToken: cancellationToken
        );

        return new GetAttachmentUploadUrlResponse
        (
            UploadUrl: presignedUploadUrl.Url,
            FileKey: fileKey,
            ExpiresAt: presignedUploadUrl.ExpiresAt
        );
    }
}