using System.Text;

using Contracts.IntegrationEvents.Chat;
using Contracts.IntegrationEvents.EphemeralChat;

using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Instructions;
using Main.Application.Abstractions.Memory;
using Main.Application.Abstractions.Stream;
using Main.Domain.Enums;
using Main.Infrastructure.AI.Helpers;
using Main.Infrastructure.AI.Plugins;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using OpenAI;
using OpenAI.Chat;

using SharedKernel;
using SharedKernel.Application.Messaging;

using StackExchange.Redis;

namespace Main.Infrastructure.AI;

internal sealed class NativeChatCompletionService(
    OpenAIClient openAiClient,
    IModelRegistry modelRegistry,
    IStreamPublisher streamPublisher,
    IMessageBus messageBus,
    IChatLockService chatLockService,
    IInstructionStore instructionStore,
    IMemoryStore memoryStore,
    Kernel kernel,
    PluginUserContext pluginUserContext,
    PluginStreamContext pluginStreamContext,
    IDateTimeProvider dateTimeProvider,
    ILogger<NativeChatCompletionService> logger
) : INativeChatCompletionService
{
    private static readonly TimeSpan StreamExpiration = TimeSpan.FromHours(1);

    public Task StreamCompletionAsync(string chatId, string streamId, string modelId, string correlationId, IReadOnlyList<ChatCompletionMessage> messages,
        CancellationToken cancellationToken)
    {
        return ExecuteStreamingAsync
        (
            chatId: chatId,
            streamId: streamId,
            modelId: modelId,
            messages: messages,
            correlationId: correlationId,
            userId: null,
            webSearchEnabled: false,
            cancellationToken: cancellationToken
        );
    }

    public Task StreamCompletionAdvancedAsync
    (
        Guid userId,
        string chatId,
        string streamId,
        string modelId,
        string correlationId,
        bool webSearchEnabled,
        IReadOnlyList<ChatCompletionMessage> messages,
        CancellationToken cancellationToken
    )
    {
        return ExecuteStreamingAsync
        (
            chatId: chatId,
            streamId: streamId,
            modelId: modelId,
            messages: messages,
            correlationId: correlationId,
            userId: userId,
            webSearchEnabled: webSearchEnabled,
            cancellationToken: cancellationToken
        );
    }

    private async Task ExecuteStreamingAsync
    (
        string chatId,
        string streamId,
        string modelId,
        string correlationId,
        bool webSearchEnabled,
        IReadOnlyList<ChatCompletionMessage> messages,
        Guid? userId,
        CancellationToken cancellationToken
    )
    {
        StringBuilder messageContent = new();

        try
        {
            await InitializeStreamAsync(streamId, cancellationToken);

            if (userId is not null)
            {
                pluginUserContext.UserId = userId.Value;

                await StreamWithToolsAsync
                (
                    streamId: streamId,
                    modelId: modelId,
                    messages: messages,
                    userId: userId.Value,
                    webSearchEnabled: webSearchEnabled,
                    messageContent: messageContent,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await StreamSimpleAsync
                (
                    streamId: streamId,
                    modelId: modelId,
                    messages: messages,
                    messageContent: messageContent,
                    cancellationToken: cancellationToken
                );
            }

            await FinalizeStreamAsync
            (
                streamId: streamId,
                chatId: chatId,
                messageContent: messageContent,
                isAdvanced: userId is not null,
                cancellationToken: cancellationToken
            );
        }
        catch (ArgumentOutOfRangeException exception) when (exception.ParamName == "value")
        {
            // OpenAI SDK throws when it encounters a finish_reason it doesn't recognise.
            // This happens with OpenRouter-proxied models (e.g. Gemini MALFORMED_FUNCTION_CALL → "error").
            // Retrying won't help — mark stream as failed and swallow so MassTransit doesn't retry.
            logger.LogWarning(exception,
                "Model returned an unrecognized finish reason for chat {ChatId}. The model may not support function calling.",
                chatId);
            await HandleStreamingErrorAsync(chatId, streamId, exception, cancellationToken);
        }
        catch (Exception exception)
        {
            await HandleStreamingErrorAsync(chatId, streamId, exception, cancellationToken);
            throw;
        }
        finally
        {
            await ReleaseChatLockAsync(chatId, correlationId);
        }
    }

    private async Task StreamWithToolsAsync
    (
        string streamId,
        string modelId,
        bool webSearchEnabled,
        IReadOnlyList<ChatCompletionMessage> messages,
        Guid userId,
        StringBuilder messageContent,
        CancellationToken cancellationToken
    )
    {
        ModelInfo? modelInfo = modelRegistry.GetModelInfo(modelId);
        bool supportsFunctionCalling = modelInfo?.ModelCapabilities.SupportsFunctionCalling ?? true;
        bool memoryToolsEnabled = supportsFunctionCalling;
        bool webSearchToolEnabled = supportsFunctionCalling && webSearchEnabled;

        ChatHistory chatHistory = await BuildChatHistoryAsync
        (
            messages: messages,
            userId: userId,
            modelInfo: modelInfo,
            memoryToolsEnabled: memoryToolsEnabled,
            webSearchToolEnabled: webSearchToolEnabled,
            cancellationToken: cancellationToken
        );

        string openRouterId = modelRegistry.GetOpenRouterModelId(modelId);

        pluginStreamContext.StreamId = streamId;

        List<KernelFunction> functions = supportsFunctionCalling
            ? kernel.Plugins
                .GetFunctionsMetadata()
                .Where(f => webSearchToolEnabled || f.PluginName != "search")
                .Select(f => kernel.Plugins.GetFunction(f.PluginName, f.Name))
                .ToList()
            : [];

        OpenAIPromptExecutionSettings settings = new()
        {
            ModelId = openRouterId,
            FunctionChoiceBehavior = functions.Count > 0
                ? FunctionChoiceBehavior.Auto
                    (
                        functions: functions,
                        autoInvoke: true,
                        options: new FunctionChoiceBehaviorOptions
                        {
                            AllowConcurrentInvocation = false,
                            AllowParallelCalls = true
                        }
                    )
                : FunctionChoiceBehavior.None(),
            MaxTokens = null
        };

        OpenAIChatCompletionService chatService = GetSkChatService(openRouterId);

#pragma warning disable S3267
        await foreach (StreamingChatMessageContent chunk in chatService.GetStreamingChatMessageContentsAsync(
                           chatHistory: chatHistory, executionSettings: settings, kernel: kernel,
                           cancellationToken: cancellationToken))
#pragma warning restore S3267
        {
            if (string.IsNullOrWhiteSpace(chunk.Content))
                continue;

            messageContent.Append(chunk.Content);
            await streamPublisher.PublishChunkAsync(streamId, chunk.Content, cancellationToken);
        }
    }

    private async Task StreamSimpleAsync
    (
        string streamId,
        string modelId,
        IReadOnlyList<ChatCompletionMessage> messages,
        StringBuilder messageContent,
        CancellationToken cancellationToken
    )
    {
        ChatClient chatClient = GetChatClient(modelId);

        List<ChatMessage> chatMessages = messages
            .Select(ChatMessageExtensions.ConvertToChatMessage)
            .ToList();

        await foreach (StreamingChatCompletionUpdate update in chatClient.CompleteChatStreamingAsync(chatMessages,
                           cancellationToken: cancellationToken))
        {
#pragma warning disable S3267
            foreach (ChatMessageContentPart? part in update.ContentUpdate)
#pragma warning restore S3267
            {
                string? chunk = part.Text;

                if (string.IsNullOrWhiteSpace(chunk))
                    continue;

                messageContent.Append(chunk);

                await streamPublisher.PublishChunkAsync(streamId, chunk, cancellationToken);
            }
        }
    }

    private async Task<ChatHistory> BuildChatHistoryAsync
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

        ChatHistory chatHistory = new ChatHistory(systemPrompt);

        foreach (ChatCompletionMessage message in messages)
        {
            switch (message.Role)
            {
                case MessageRole.User:
                    chatHistory.AddUserMessage(message.Content);
                    break;
                case MessageRole.Assistant:
                    chatHistory.AddAssistantMessage(message.Content);
                    break;
            }
        }

        return chatHistory;
    }

    private OpenAIChatCompletionService GetSkChatService(string openRouterId)
    {
        return new OpenAIChatCompletionService
        (
            modelId: openRouterId,
            openAIClient: openAiClient
        );
    }

    private ChatClient GetChatClient(string modelId)
    {
        string openRouterId = modelRegistry.GetOpenRouterModelId(modelId);
        return openAiClient.GetChatClient(openRouterId);
    }

    private async Task InitializeStreamAsync(string streamId, CancellationToken cancellationToken)
    {
        await streamPublisher.PublishStatusAsync(streamId, StreamStatus.Pending, cancellationToken);
        await streamPublisher.SetStreamExpirationAsync(streamId, StreamExpiration, cancellationToken);
    }

    private async Task FinalizeStreamAsync
    (
        string streamId,
        string chatId,
        StringBuilder messageContent,
        bool isAdvanced,
        CancellationToken cancellationToken
    )
    {
        await streamPublisher.PublishStatusAsync(streamId, StreamStatus.Done, cancellationToken);

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
                    MessageContent = content
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
                    MessageContent = content
                }, cancellationToken
            );
        }
    }

    private async Task HandleStreamingErrorAsync
    (
        string chatId,
        string streamId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Streaming failed for chat {ChatId}", chatId);

        try
        {
            await streamPublisher.PublishStatusAsync
            (
                streamId: streamId,
                status: StreamStatus.Failed,
                fault: exception.Message,
                cancellationToken: cancellationToken
            );
        }
        catch (RedisException redisException)
        {
            logger.LogError(redisException,
                "Failed to publish faulted status for chat {ChatId}", chatId);
        }
    }

    private async Task ReleaseChatLockAsync(string chatId, string ownerId)
    {
        try
        {
            await chatLockService.ReleaseLockAsync
            (
                chatId: chatId,
                ownerId: ownerId,
                cancellationToken: CancellationToken.None
            );
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Failed to release lock for chat {ChatId}", chatId);
        }
    }
}