using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Memory;

using Microsoft.SemanticKernel.ChatCompletion;

namespace Main.Infrastructure.AI.Helpers.Interfaces;

internal interface IChatHistoryBuilder
{
    Task<ChatHistoryResult> BuildAsync
    (
        IReadOnlyList<ChatCompletionMessage> messages,
        Guid userId,
        ModelInfo? modelInfo,
        bool memoryToolsEnabled,
        bool webSearchToolEnabled,
        CancellationToken cancellationToken
    );
}

internal sealed record ChatHistoryResult
(
    ChatHistory ChatHistory,
    IReadOnlyList<MemoryEntry> MemoryEntries
);