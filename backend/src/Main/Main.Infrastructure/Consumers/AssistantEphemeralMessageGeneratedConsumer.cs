using Contracts.IntegrationEvents.EphemeralChat;

using Main.Application.Abstractions.Ephemeral;
using Main.Domain.Enums;
using Main.Domain.Models;

using MassTransit;

using Microsoft.Extensions.Logging;

namespace Main.Infrastructure.Consumers;

internal sealed class AssistantEphemeralMessageGeneratedConsumer(IEphemeralChatStore ephemeralChatStore, ILogger<AssistantEphemeralMessageGeneratedConsumer> logger) : IConsumer<AssistantEphemeralMessageGenerated>
{
    public async Task Consume(ConsumeContext<AssistantEphemeralMessageGenerated> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        AssistantEphemeralMessageGenerated message = context.Message;

        string ephemeralChatId = message.EphemeralChatId;

        if (string.IsNullOrWhiteSpace(message.MessageContent))
        {
            logger.LogError(
                "Empty MessageContent in {EventType}: {EventId}, CorrelationId: {CorrelationId}, EphemeralChatId: {EphemeralChatId}",
                nameof(AssistantEphemeralMessageGenerated), message.EventId, message.CorrelationId, ephemeralChatId);
            return;
        }

        EphemeralChat? ephemeralChat = await ephemeralChatStore.GetAsync(ephemeralChatId, cancellationToken);

        if (ephemeralChat is null)
        {
            logger.LogError(
                "EphemeralChat not found in {EventType}: {EventId}, CorrelationId: {CorrelationId}, EphemeralChatId: {EphemeralChatId}",
                nameof(AssistantEphemeralMessageGenerated), message.EventId, message.CorrelationId, ephemeralChatId);
            return;
        }

        int nextSequence = ephemeralChat.Messages
            .Select(m => m.SequenceNumber)
            .DefaultIfEmpty(-1)
            .Max() + 1;

        EphemeralMessage ephemeralMessage = new()
        {
            MessageRole = MessageRole.Assistant,
            MessageContent = message.MessageContent,
            SequenceNumber = nextSequence
        };

        ephemeralChat.Messages.Add(ephemeralMessage);

        await ephemeralChatStore.SaveAsync(ephemeralChat, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, EphemeralChatId: {EphemeralChatId}",
                nameof(AssistantEphemeralMessageGenerated), message.EventId, message.CorrelationId, ephemeralChatId);
    }
}