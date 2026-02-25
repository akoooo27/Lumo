using Auth.Application.Abstractions.Data;
using Auth.Application.Abstractions.Users;
using Auth.Application.Faults;
using Auth.Domain.Aggregates;
using Auth.Domain.ValueObjects;

using Contracts.IntegrationEvents.Auth;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.Users.CancelDeletion;

internal sealed class CancelUserDeletionHandler(
    IAuthDbContext dbContext,
    IUserContext userContext,
    IRequestContext requestContext,
    IMessageBus messageBus,
    ICurrentUserReadStore currentUserReadStore,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CancelUserDeletionCommand, CancelUserDeletionResponse>
{
    public async ValueTask<Outcome<CancelUserDeletionResponse>> Handle(CancelUserDeletionCommand request, CancellationToken cancellationToken)
    {
        Outcome<UserId> userIdOutcome = UserId.FromGuid(userContext.UserId);

        if (userIdOutcome.IsFailure)
            return userIdOutcome.Fault;

        UserId userId = userIdOutcome.Value;

        User? user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return UserOperationFaults.NotFound;

        Outcome cancelDeletionOutcome = user.CancelDeletion(dateTimeProvider.UtcNow);

        if (cancelDeletionOutcome.IsFailure)
            return cancelDeletionOutcome.Fault;

        Outcome<Fingerprint> fingerprintOutcome = Fingerprint.Create
        (
            ipAddress: requestContext.IpAddress,
            userAgent: requestContext.UserAgent,
            timezone: requestContext.Timezone,
            language: requestContext.Language,
            normalizedBrowser: requestContext.NormalizedBrowser,
            normalizedOs: requestContext.NormalizedOs
        );

        if (fingerprintOutcome.IsFailure)
            return fingerprintOutcome.Fault;

        UserDeletionCanceled userDeletionCanceled = new()
        {
            EventId = Guid.NewGuid(),
            OccurredAt = dateTimeProvider.UtcNow,
            CorrelationId = Guid.Parse(requestContext.CorrelationId),
            UserId = user.Id.Value,
            EmailAddress = user.EmailAddress.Value,
            IpAddress = fingerprintOutcome.Value.IpAddress,
            UserAgent = fingerprintOutcome.Value.UserAgent
        };

        await messageBus.PublishAsync(userDeletionCanceled, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await currentUserReadStore.InvalidateCacheAsync(userId.Value, cancellationToken);

        CancelUserDeletionResponse response = new
        (
            Id: user.Id.Value,
            CanceledAt: userDeletionCanceled.OccurredAt
        );

        return response;
    }
}