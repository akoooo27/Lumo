using Auth.Application.Abstractions.Data;
using Auth.Application.Abstractions.Storage;
using Auth.Application.Abstractions.Users;
using Auth.Application.Faults;
using Auth.Domain.Aggregates;
using Auth.Domain.ValueObjects;

using Contracts.IntegrationEvents.Auth;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.Users.UpdateProfile;

internal sealed class UpdateProfileHandler(
    IAuthDbContext dbContext,
    IUserContext userContext,
    IRequestContext requestContext,
    IStorageService storageService,
    IMessageBus messageBus,
    ICurrentUserReadStore currentUserReadStore,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateProfileCommand>
{
    public async ValueTask<Outcome> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        Outcome<UserId> userIdOutcome = UserId.FromGuid(userContext.UserId);

        if (userIdOutcome.IsFailure)
            return userIdOutcome.Fault;

        UserId userId = userIdOutcome.Value;

        User? user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return UserOperationFaults.NotFound;

        if (request.NewDisplayName is not null)
        {
            Outcome changeDisplayNameOutcome = user.ChangeDisplayName
            (
                newDisplayName: request.NewDisplayName,
                utcNow: dateTimeProvider.UtcNow
            );

            if (changeDisplayNameOutcome.IsFailure)
                return changeDisplayNameOutcome.Fault;

            UserDisplayNameChanged userDisplayNameChanged = new()
            {
                EventId = Guid.NewGuid(),
                OccurredAt = dateTimeProvider.UtcNow,
                CorrelationId = Guid.Parse(requestContext.CorrelationId),
                UserId = user.Id.Value,
                DisplayName = user.DisplayName
            };

            await messageBus.PublishAsync(userDisplayNameChanged, cancellationToken);
        }

        if (request.NewAvatarKey is not null)
        {
            bool isOwned = await storageService.IsOwnedByAsync
            (
                request.NewAvatarKey,
                userId.Value,
                cancellationToken
            );

            if (!isOwned)
                return UserOperationFaults.AvatarForbidden;

            bool fileExists = await storageService.FileExistsAsync(request.NewAvatarKey, cancellationToken);

            if (!fileExists)
                return UserOperationFaults.AvatarNotFound;

            Outcome setAvatarKeyOutcome = user.SetAvatarKey
            (
                avatarKey: request.NewAvatarKey,
                utcNow: dateTimeProvider.UtcNow
            );

            if (setAvatarKeyOutcome.IsFailure)
                return setAvatarKeyOutcome.Fault;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await currentUserReadStore.InvalidateCacheAsync(userId.Value, cancellationToken);

        return Outcome.Success();
    }
}