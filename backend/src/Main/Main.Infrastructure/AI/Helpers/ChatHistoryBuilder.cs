using System.Diagnostics.CodeAnalysis;

using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Instructions;
using Main.Application.Abstractions.Memory;
using Main.Application.Abstractions.Storage;
using Main.Domain.Enums;
using Main.Infrastructure.AI.Helpers.Interfaces;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using SharedKernel;

namespace Main.Infrastructure.AI.Helpers;

internal sealed class ChatHistoryBuilder(
    IInstructionStore instructionStore,
    IMemoryStore memoryStore,
    IStorageService storageService,
    IDateTimeProvider dateTimeProvider) : IChatHistoryBuilder
{
    private static readonly TimeSpan ReadUrlExpiration = TimeSpan.FromMinutes(15);

    [Experimental("SKEXP0001")]
    public async Task<ChatHistoryResult> BuildAsync
    (
        IReadOnlyList<ChatCompletionMessage> messages,
        Guid userId,
        ModelInfo? modelInfo,
        bool memoryToolsEnabled,
        bool webSearchToolEnabled,
        CancellationToken cancellationToken
    )
    {

        string latestUserMessage = messages
            .Where(m => m.Role == MessageRole.User)
            .Select(m => m.Content)
            .LastOrDefault() ?? string.Empty;

        IReadOnlyList<MemoryEntry> memories = await memoryStore.GetRelevantAsync(
            userId, latestUserMessage, MemoryConstants.MaxMemoriesInContext, cancellationToken);

        IReadOnlyList<InstructionEntry> instructions = await instructionStore
            .GetForUserAsync(userId, cancellationToken);

        string systemPrompt = SystemPromptBuilder.Build
        (
            instructions: instructions,
            memories: memories,
            modelInfo: modelInfo,
            memoryToolsEnabled: memoryToolsEnabled,
            webSearchToolEnabled: webSearchToolEnabled,
            dateTimeProvider: dateTimeProvider
        );

        ChatHistory chatHistory = new(systemPrompt);

        foreach (ChatCompletionMessage message in messages)
        {
            switch (message.Role)
            {
                case MessageRole.User when message.AttachmentFileKey is not null:
                    string readUrl = await storageService.GetPresignedReadUrlAsync
                    (
                        fileKey: message.AttachmentFileKey,
                        expiration: ReadUrlExpiration,
                        cancellationToken: cancellationToken
                    );
                    if (message.AttachmentContentType == "application/pdf")
                    {
                        byte[] pdfBytes =
                            await storageService.DownloadFileAsync(message.AttachmentFileKey, cancellationToken);

                        chatHistory.AddUserMessage(contentItems:
                        [
                            new TextContent(message.Content),
                            new BinaryContent(pdfBytes, "application/pdf"),
                        ]);
                    }
                    else
                    {
                        chatHistory.AddUserMessage
                        (
                            contentItems:
                            [
                                new TextContent(message.Content),
                                new ImageContent(new Uri(readUrl))
                            ]);
                    }

                    break;

                case MessageRole.User:
                    chatHistory.AddUserMessage(message.Content);
                    break;
                case MessageRole.Assistant:
                    chatHistory.AddAssistantMessage(message.Content);
                    break;
            }
        }

        ChatHistoryResult chatHistoryResult = new
        (
            ChatHistory: chatHistory,
            MemoryEntries: memories
        );

        return chatHistoryResult;
    }
}