using Contracts.IntegrationEvents.Auth;

using MassTransit;

using Microsoft.EntityFrameworkCore;

using Notifications.Api.Data;

namespace Notifications.Api.Consumers;

internal sealed class UserDeletedConsumer(INotificationDbContext dbContext, ILogger<UserDeletedConsumer> logger)
    : IConsumer<UserDeleted>
{
    public async Task Consume(ConsumeContext<UserDeleted> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        UserDeleted message = context.Message;

        await dbContext.Notifications
            .Where(n => n.UserId == message.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Users
            .Where(u => u.UserId == message.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}, UserId: {UserId}",
                nameof(UserDeleted), message.EventId, message.CorrelationId, message.OccurredAt, message.UserId);
    }
}