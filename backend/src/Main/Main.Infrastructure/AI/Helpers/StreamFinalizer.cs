using System.Text;

using Contracts.IntegrationEvents.Chat;
using Contracts.IntegrationEvents.EphemeralChat;

using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Stream;
using Main.Infrastructure.AI.Helpers.Interfaces;
using Main.Infrastructure.AI.Plugins;

using SharedKernel;
using SharedKernel.Application.Messaging;

namespace Main.Infrastructure.AI.Helpers;

internal sealed class StreamFinalizer(
    IModelRegistry modelRegistry,
    IStreamPublisher streamPublisher,
    IMessageBus messageBus,
    PluginStreamContext pluginStreamContext,
    IDateTimeProvider dateTimeProvider
) : IStreamFinalizer
{
    public async Task FinalizeAsync
    (
        string streamId,
        string chatId,
        string modelId,
        StringBuilder messageContent,
        TokenUsage tokenUsage,
        bool isAdvanced,
        CancellationToken cancellationToken
    )
    {
        ModelInfo? modelInfo = modelRegistry.GetModelInfo(modelId);

        await streamPublisher.PublishStatusAsync
        (
            streamId: streamId,
            status: StreamStatus.Done,
            cancellationToken: cancellationToken,
            modelName: modelInfo?.DisplayName,
            provider: modelInfo?.Provider
        );

        if (messageContent.Length == 0)
            return;

        string content = messageContent.ToString();

        if (isAdvanced)
        {
            await messageBus.PublishAsync
            (
                new AssistantMessageGenerated
                {
                    EventId = Guid.NewGuid(),
                    OccurredAt = dateTimeProvider.UtcNow,
                    CorrelationId = Guid.NewGuid(),
                    ChatId = chatId,
                    MessageContent = content,
                    InputTokens = tokenUsage.InputTokens > 0 ? tokenUsage.InputTokens : null,
                    OutputTokens = tokenUsage.OutputTokens > 0 ? tokenUsage.OutputTokens : null,
                    TotalTokens = tokenUsage.TotalTokens > 0 ? tokenUsage.TotalTokens : null,
                    ModelId = modelId,
                    SourcesJson = pluginStreamContext.SourcesJson
                }, cancellationToken
            );
        }
        else
        {
            await messageBus.PublishAsync
            (
                new AssistantEphemeralMessageGenerated
                {
                    EventId = Guid.NewGuid(),
                    OccurredAt = dateTimeProvider.UtcNow,
                    CorrelationId = Guid.NewGuid(),
                    EphemeralChatId = chatId,
                    MessageContent = content,
                    InputTokens = tokenUsage.InputTokens > 0 ? tokenUsage.InputTokens : null,
                    OutputTokens = tokenUsage.OutputTokens > 0 ? tokenUsage.OutputTokens : null,
                    TotalTokens = tokenUsage.TotalTokens > 0 ? tokenUsage.TotalTokens : null,
                    ModelId = modelId
                }, cancellationToken
            );
        }
    }
}