using Contracts.IntegrationEvents.Chat;

using Main.Application.Abstractions.Storage;

using MassTransit;

using Microsoft.Extensions.Logging;

namespace Main.Infrastructure.Consumers;

internal sealed class ChatDeletedConsumer(IStorageService storageService, ILogger<ChatDeletedConsumer> logger) : IConsumer<ChatDeleted>
{
    public async Task Consume(ConsumeContext<ChatDeleted> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        ChatDeleted message = context.Message;

        foreach (string fileKey in message.AttachmentFileKeys)
            await storageService.DeleteFileAsync(fileKey, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}, ChatId: {ChatId}, DeletedAttachments: {Count}",
                nameof(ChatDeleted), message.EventId, message.CorrelationId, message.OccurredAt, message.ChatId,
                message.AttachmentFileKeys.Count);
    }
}