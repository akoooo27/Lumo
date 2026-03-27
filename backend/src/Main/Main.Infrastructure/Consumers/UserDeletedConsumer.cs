using Contracts.IntegrationEvents.Auth;

using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Storage;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Main.Infrastructure.Consumers;

internal sealed class UserDeletedConsumer(
    IMainDbContext dbContext,
    IStorageService storageService,
    ILogger<UserDeletedConsumer> logger)
    : IConsumer<UserDeleted>
{
    public async Task Consume(ConsumeContext<UserDeleted> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        UserDeleted message = context.Message;

        string attachmentPrefix = $"{AttachmentConstants.AttachmentFolder}/{message.UserId:N}/";
        await storageService.DeleteByPrefixAsync(attachmentPrefix, cancellationToken);

        await dbContext.Chats
            .Where(c => c.UserId == message.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Preferences
            .Where(p => p.UserId == message.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Workflows
            .Where(w => w.UserId == message.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.SharedChats
            .Where(sc => sc.OwnerId == message.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Folders
            .Where(f => f.UserId == message.UserId)
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