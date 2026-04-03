using Contracts.IntegrationEvents.Chat;

using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Data;
using Main.Domain.Constants;
using Main.Domain.ValueObjects;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel;

namespace Main.Infrastructure.Consumers;

internal sealed class ChatStartedConsumer(
    IMainDbContext dbContext,
    INativeChatCompletionService nativeChatCompletionService,
    ILogger<ChatStartedConsumer> logger) : IConsumer<ChatStarted>
{
    public async Task Consume(ConsumeContext<ChatStarted> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        ChatStarted message = context.Message;

        Outcome<ChatId> chatIdOutcome = ChatId.From(message.ChatId);

        if (chatIdOutcome.IsFailure)
        {
            logger.LogError(
                "Invalid ChatId in {EventType}: {EventId}, CorrelationId: {CorrelationId}, ChatId: {ChatId}",
                nameof(ChatStarted), message.EventId, message.CorrelationId, message.ChatId);
            return;
        }

        ChatId chatId = chatIdOutcome.Value;

        Outcome<StreamId> streamIdOutcome = StreamId.From(message.StreamId);

        if (streamIdOutcome.IsFailure)
        {
            logger.LogError(
                "Invalid StreamId in {EventType}: {EventId}, CorrelationId: {CorrelationId}, StreamId: {StreamId}",
                nameof(ChatStarted), message.EventId, message.CorrelationId, message.StreamId);
            return;
        }

        StreamId streamId = streamIdOutcome.Value;

        List<ChatCompletionMessage> messages = await dbContext.Messages
            .Where(c => c.ChatId == chatId)
            .OrderByDescending(c => c.SequenceNumber)
            .Take(ChatConstants.MaxContextMessages)
            .OrderBy(c => c.SequenceNumber)
            .Select(m => new ChatCompletionMessage
            (
                Role: m.MessageRole,
                Content: m.MessageContent,
                AttachmentFileKey: m.Attachment != null ? m.Attachment.FileKey : null,
                AttachmentContentType: m.Attachment != null ? m.Attachment.ContentType : null
            ))
            .ToListAsync(cancellationToken);

        await nativeChatCompletionService.StreamCompletionAdvancedAsync
        (
            chatId: chatId.Value,
            streamId: streamId.Value,
            messages: messages,
            modelId: message.ModelId,
            userId: message.UserId,
            webSearchEnabled: message.WebSearchEnabled,
            correlationId: message.CorrelationId.ToString(),
            cancellationToken: cancellationToken
        );

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}, ChatId: {ChatId}",
                nameof(ChatStarted), message.EventId, message.CorrelationId, message.OccurredAt, message.ChatId);
    }
}