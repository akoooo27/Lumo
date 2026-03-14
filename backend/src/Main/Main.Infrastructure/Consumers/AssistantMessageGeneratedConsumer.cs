using Contracts.IntegrationEvents.Chat;

using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Generators;
using Main.Domain.Aggregates;
using Main.Domain.ValueObjects;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel;

namespace Main.Infrastructure.Consumers;

internal sealed class AssistantMessageGeneratedConsumer(
    IMainDbContext dbContext,
    IIdGenerator idGenerator,
    ILogger<AssistantMessageGeneratedConsumer> logger) : IConsumer<AssistantMessageGenerated>
{
    public async Task Consume(ConsumeContext<AssistantMessageGenerated> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        AssistantMessageGenerated message = context.Message;

        Outcome<ChatId> chatIdOutcome = ChatId.From(message.ChatId);

        if (chatIdOutcome.IsFailure)
        {
            logger.LogError(
                "Invalid ChatId in {EventType}: {EventId}, CorrelationId: {CorrelationId}, ChatId: {ChatId}",
                nameof(AssistantMessageGenerated), message.EventId, message.CorrelationId, message.ChatId);
            return;
        }

        ChatId chatId = chatIdOutcome.Value;

        if (string.IsNullOrWhiteSpace(message.MessageContent))
        {
            logger.LogError(
                "Empty MessageContent in {EventType}: {EventId}, CorrelationId: {CorrelationId}, ChatId: {ChatId}",
                nameof(AssistantMessageGenerated), message.EventId, message.CorrelationId, message.ChatId);
            return;
        }

        Chat? chat = await dbContext.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId, cancellationToken);

        if (chat is null)
        {
            logger.LogError(
                "Chat not found in {EventType}: {EventId}, CorrelationId: {CorrelationId}, ChatId: {ChatId}",
                nameof(AssistantMessageGenerated), message.EventId, message.CorrelationId, message.ChatId);
            return;
        }

        MessageId messageId = idGenerator.NewMessageId();

        Outcome messageOutcome = chat.AddAssistantMessage
        (
            messageId: messageId,
            messageContent: message.MessageContent,
            utcNow: message.OccurredAt
        );

        if (messageOutcome.IsFailure)
        {
            logger.LogError(
                "Failed to create message in {EventType}: {EventId}, CorrelationId: {CorrelationId}, ChatId: {ChatId}, Fault: {Fault}",
                nameof(AssistantMessageGenerated), message.EventId, message.CorrelationId, message.ChatId,
                messageOutcome.Fault);
            return;
        }

        if (message.TotalTokens is > 0)
        {
            Outcome setTokensOutcome = chat.SetMessageTokenUsage
            (
                messageId: messageId,
                inputTokenCount: message.InputTokens ?? 0,
                outputTokenCount: message.OutputTokens ?? 0,
                totalTokenCount: message.TotalTokens.Value
            );

            if (setTokensOutcome.IsFailure)
            {
                logger.LogError(
                    "Failed to set token usage in {EventType}: {EventId}, CorrelationId: {CorrelationId}, ChatId: {ChatId}, Fault: {Fault}",
                    nameof(AssistantMessageGenerated), message.EventId, message.CorrelationId, message.ChatId,
                    setTokensOutcome.Fault);
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(message.SourcesJson))
        {
            Outcome setSourcesOutcome = chat.SetMessageSources
            (
                messageId: messageId,
                sourcesJson: message.SourcesJson
            );

            if (setSourcesOutcome.IsFailure)
            {
                logger.LogError(
                    "Failed to set message sources in {EventType}: {EventId}, CorrelationId: {CorrelationId}, ChatId: {ChatId}, Fault: {Fault}",
                    nameof(AssistantMessageGenerated), message.EventId, message.CorrelationId, message.ChatId,
                    setSourcesOutcome.Fault);
                return;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, ChatId: {ChatId}",
                nameof(AssistantMessageGenerated), message.EventId, message.CorrelationId, message.ChatId);
    }
}