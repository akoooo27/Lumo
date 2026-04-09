using System.Diagnostics.CodeAnalysis;

using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Instructions;
using Main.Application.Abstractions.Memory;
using Main.Application.Abstractions.Storage;
using Main.Domain.Enums;
using Main.Domain.ValueObjects;
using Main.Infrastructure.AI.Helpers.Interfaces;
using Main.Infrastructure.Caching;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using SharedKernel;

using Attachment = Main.Domain.ValueObjects.Attachment;

namespace Main.Infrastructure.AI.Helpers;

internal sealed class ChatHistoryBuilder(
    IInstructionStore instructionStore,
    IStorageService storageService,
    IFileCache fileCache,
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
        IReadOnlyList<InstructionEntry> instructions = await instructionStore
            .GetForUserAsync(userId, cancellationToken);

        SystemPromptParts parts = SystemPromptBuilder.Build
        (
            instructions: instructions,
            modelInfo: modelInfo,
            memoryToolsEnabled: memoryToolsEnabled,
            webSearchToolEnabled: webSearchToolEnabled,
            dateTimeProvider: dateTimeProvider
        );

        ChatHistory chatHistory = new(parts.Core);

        if (parts.UserInstructions is not null)
            chatHistory.AddSystemMessage(parts.UserInstructions);

        if (parts.ToolGuidance is not null)
            chatHistory.AddSystemMessage(parts.ToolGuidance);

        foreach (ChatCompletionMessage message in messages)
        {
            switch (message.Role)
            {
                case MessageRole.User when message.AttachmentFileKey is not null:
                    bool requiresBinary =
                        Attachment.TryGetContentTypeInfo(message.AttachmentContentType!, out ContentTypeInfo info)
                        && info.RequiresBinaryDelivery;
                    if (requiresBinary)
                    {
                        try
                        {
                            byte[] pdfBytes = await fileCache.GetOrSetAsync
                            (
                                key: message.AttachmentFileKey,
                                factory: async () =>
                                {
                                    byte[] bytes = await storageService.DownloadFileAsync
                                    (
                                        fileKey: message.AttachmentFileKey,
                                        cancellationToken: cancellationToken
                                    );
                                    return bytes;
                                },
                                cancellationToken: cancellationToken
                            );

                            chatHistory.AddUserMessage(contentItems:
                            [
                                new TextContent(message.Content),
                                new BinaryContent(pdfBytes, "application/pdf"),
                            ]);
                        }
                        catch (IOException)
                        {
                            string errorMessage =
                                "The file attached to the user's message could not be retrieved. Please try again later.";

                            chatHistory.AddUserMessage(contentItems:
                            [
                                new TextContent(message.Content),
                                new TextContent(errorMessage)
                            ]);
                        }
                    }
                    else
                    {
                        string readUrl = await storageService.GetPresignedReadUrlAsync
                        (
                            fileKey: message.AttachmentFileKey,
                            expiration: ReadUrlExpiration,
                            cancellationToken: cancellationToken
                        );

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

        ChatHistoryResult chatHistoryResult = new(chatHistory);

        return chatHistoryResult;
    }
}