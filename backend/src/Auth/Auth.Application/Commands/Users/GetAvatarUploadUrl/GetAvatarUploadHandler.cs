using Auth.Application.Abstractions.Data;
using Auth.Application.Abstractions.Storage;
using Auth.Application.Faults;
using Auth.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;
using SharedKernel.Application.Storage;

namespace Auth.Application.Commands.Users.GetAvatarUploadUrl;

internal sealed class GetAvatarUploadHandler(
    IAuthDbContext dbContext,
    IUserContext userContext,
    IStorageService storageService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<GetAvatarUploadUrlCommand, GetAvatarUploadUrlResponse>
{
    public async ValueTask<Outcome<GetAvatarUploadUrlResponse>> Handle(GetAvatarUploadUrlCommand request, CancellationToken cancellationToken)
    {
        Outcome<UserId> userIdOutcome = UserId.FromGuid(userContext.UserId);

        if (userIdOutcome.IsFailure)
            return userIdOutcome.Fault;

        UserId userId = userIdOutcome.Value;

        bool userExists = await dbContext.Users
            .AnyAsync(u => u.Id == userId, cancellationToken);

        if (!userExists)
            return UserOperationFaults.NotFound;

        string avatarKey =
            $"{AvatarConstants.AvatarFolder}/{userId.Value:N}/{Guid.NewGuid():N}/{dateTimeProvider.UtcNow.Ticks}";

        PresignedUploadUrl presignedUploadUrl = await storageService.GetPresignedUploadUrlAsync
        (
            fileKey: avatarKey,
            contentType: request.ContentType,
            contentLength: request.ContentLength,
            expiration: TimeSpan.FromMinutes(AvatarConstants.PresignedUrlExpirationMinutes),
            cancellationToken: cancellationToken
        );

        return new GetAvatarUploadUrlResponse
        (
            UploadUrl: presignedUploadUrl.Url,
            AvatarKey: avatarKey,
            ExpiresAt: presignedUploadUrl.ExpiresAt
        );
    }
}