using Contracts.IntegrationEvents.Auth;

using MassTransit;

using Notifications.Api.Data;
using Notifications.Api.ReadModels;

namespace Notifications.Api.Consumers;

internal sealed class UserSignedUpConsumer(INotificationDbContext dbContext, ILogger<UserSignedUpConsumer> logger)
    : IConsumer<UserSignedUp>
{
    public async Task Consume(ConsumeContext<UserSignedUp> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        UserSignedUp message = context.Message;

        User newUser = new()
        {
            UserId = message.UserId,
            DisplayName = message.DisplayName,
            EmailAddress = message.EmailAddress,
        };

        await dbContext.Users.AddAsync(newUser, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}, UserId: {UserId}",
                nameof(UserSignedUp), message.EventId, message.CorrelationId, message.OccurredAt, message.UserId);
    }
}