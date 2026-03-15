using Main.Application.Abstractions.AI;

using Microsoft.SemanticKernel.ChatCompletion;

namespace Main.Infrastructure.AI.Helpers.Interfaces;

internal interface IChatHistoryBuilder
{
    Task<ChatHistory> BuildAsync
    (
        IReadOnlyList<ChatCompletionMessage> messages,
        Guid userId,
        ModelInfo? modelInfo,
        bool memoryToolsEnabled,
        bool webSearchToolEnabled,
        CancellationToken cancellationToken
    );
}