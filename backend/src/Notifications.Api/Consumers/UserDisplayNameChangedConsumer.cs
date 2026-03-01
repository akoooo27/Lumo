using Contracts.IntegrationEvents.Auth;

using MassTransit;

using Microsoft.EntityFrameworkCore;

using Notifications.Api.Data;

namespace Notifications.Api.Consumers;

internal sealed class UserDisplayNameChangedConsumer(INotificationDbContext dbContext, ILogger<UserDisplayNameChangedConsumer> logger)
    : IConsumer<UserDisplayNameChanged>
{
    public async Task Consume(ConsumeContext<UserDisplayNameChanged> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        UserDisplayNameChanged message = context.Message;

        int rowsAffected = await dbContext.Users
            .Where(u => u.UserId == message.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.DisplayName, message.DisplayName), cancellationToken);

        if (rowsAffected == 0)
        {
            logger.LogWarning(
                "User with ID {UserId} not found for display name update. EventId: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}",
                message.UserId, message.EventId, message.CorrelationId, message.OccurredAt);
            return;
        }

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}, UserId: {UserId}",
                nameof(UserDisplayNameChanged), message.EventId, message.CorrelationId, message.OccurredAt, message.UserId);
    }
}