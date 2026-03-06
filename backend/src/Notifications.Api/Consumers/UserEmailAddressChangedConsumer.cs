using Contracts.IntegrationEvents.Auth;

using MassTransit;

using Microsoft.EntityFrameworkCore;

using Notifications.Api.Data;

namespace Notifications.Api.Consumers;

internal sealed class UserEmailAddressChangedConsumer(INotificationDbContext dbContext, ILogger<UserEmailAddressChangedConsumer> logger)
    : IConsumer<UserEmailAddressChanged>
{
    public async Task Consume(ConsumeContext<UserEmailAddressChanged> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        UserEmailAddressChanged message = context.Message;

        int rowsAffected = await dbContext.Users
            .Where(u => u.UserId == message.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.EmailAddress, message.NewEmailAddress), cancellationToken);

        if (rowsAffected == 0)
        {
            logger.LogWarning(
                "User with ID {UserId} not found for email address update. EventId: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}",
                message.UserId, message.EventId, message.CorrelationId, message.OccurredAt);
            return;
        }

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}, UserId: {UserId}",
                nameof(UserEmailAddressChanged), message.EventId, message.CorrelationId, message.OccurredAt, message.UserId);
    }
}