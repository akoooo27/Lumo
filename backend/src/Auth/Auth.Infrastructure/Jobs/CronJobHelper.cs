using Auth.Application.Abstractions.Data;
using Auth.Application.Abstractions.Storage;
using Auth.Domain.Aggregates;
using Auth.Domain.Constants;

using Contracts.IntegrationEvents.Auth;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Messaging;

namespace Auth.Infrastructure.Jobs;

internal sealed class CronJobHelper(
    IAuthDbContext dbContext,
    IMessageBus messageBus,
    IStorageService storageService,
    IDateTimeProvider dateTimeProvider) : ICronJobHelper
{
    public async Task HardDeleteUsersAsync(CancellationToken cancellationToken = default)
    {
        const int batchSize = 1000;
        DateTimeOffset cutOffTime = dateTimeProvider.UtcNow.AddDays(-UserConstants.AccountRecoveryPeriodInDays);

        while (true)
        {
            List<User> usersToDelete = await dbContext.Users
                .Where(user => user.DeletedAt.HasValue && user.DeletedAt <= cutOffTime)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (usersToDelete.Count == 0)
                return;

            foreach (User userToDelete in usersToDelete)
            {
                string avatarPrefix = $"{AvatarConstants.AvatarFolder}/{userToDelete.Id.Value:N}/";
                await storageService.DeleteByPrefixAsync(avatarPrefix, cancellationToken);

                await dbContext.Sessions
                    .Where(s => s.UserId == userToDelete.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.LoginRequests
                    .Where(lr => lr.UserId == userToDelete.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.EmailChangeRequests
                    .Where(ecr => ecr.UserId == userToDelete.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.RecoveryRequests
                    .Where(rr => rr.UserId == userToDelete.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.RecoveryKeyChains
                    .Where(rkc => rkc.UserId == userToDelete.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                UserDeleted userDeleted = new()
                {
                    EventId = Guid.NewGuid(),
                    OccurredAt = dateTimeProvider.UtcNow,
                    CorrelationId = Guid.NewGuid(),
                    UserId = userToDelete.Id.Value,
                    EmailAddress = userToDelete.EmailAddress.Value,
                };

                await messageBus.PublishAsync(userDeleted, cancellationToken);
            }

            dbContext.Users.RemoveRange(usersToDelete);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}