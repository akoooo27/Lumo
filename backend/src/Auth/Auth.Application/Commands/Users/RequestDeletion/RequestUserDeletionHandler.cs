using Auth.Application.Abstractions.Data;
using Auth.Application.Abstractions.Users;
using Auth.Application.Faults;
using Auth.Domain.Aggregates;
using Auth.Domain.Constants;
using Auth.Domain.ValueObjects;

using Contracts.IntegrationEvents.Auth;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.Users.RequestDeletion;

internal sealed class RequestUserDeletionHandler(
    IAuthDbContext dbContext,
    IUserContext userContext,
    IRequestContext requestContext,
    IMessageBus messageBus,
    ICurrentUserReadStore currentUserReadStore,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RequestUserDeletionCommand, RequestUserDeletionResponse>
{
    public async ValueTask<Outcome<RequestUserDeletionResponse>> Handle(RequestUserDeletionCommand request, CancellationToken cancellationToken)
    {
        Outcome<UserId> userIdOutcome = UserId.FromGuid(userContext.UserId);

        if (userIdOutcome.IsFailure)
            return userIdOutcome.Fault;

        UserId userId = userIdOutcome.Value;

        User? user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return UserOperationFaults.NotFound;

        Outcome deletionOutcome = user.MarkAsDeleted(dateTimeProvider.UtcNow);

        if (deletionOutcome.IsFailure)
            return deletionOutcome.Fault;

        DateTimeOffset deletedAt = user.DeletedAt!.Value;

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

        UserDeletionRequested deletionRequested = new()
        {
            EventId = Guid.NewGuid(),
            OccurredAt = dateTimeProvider.UtcNow,
            CorrelationId = Guid.Parse(requestContext.CorrelationId),
            UserId = user.Id.Value,
            EmailAddress = user.EmailAddress.Value,
            IpAddress = fingerprintOutcome.Value.IpAddress,
            UserAgent = fingerprintOutcome.Value.UserAgent,
            RequestedAt = deletedAt,
            WillBeDeletedAt = deletedAt.AddDays(UserConstants.AccountRecoveryPeriodInDays)
        };

        await messageBus.PublishAsync(deletionRequested, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await currentUserReadStore.InvalidateCacheAsync(userId.Value, cancellationToken);

        RequestUserDeletionResponse response = new
        (
            Id: user.Id.Value,
            RequestedAt: deletionRequested.RequestedAt,
            WillBeDeletedAt: deletionRequested.WillBeDeletedAt
        );

        return response;
    }
}