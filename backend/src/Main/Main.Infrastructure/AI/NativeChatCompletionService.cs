using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Memory;
using Main.Application.Abstractions.Services;
using Main.Application.Abstractions.Stream;
using Main.Infrastructure.AI.Helpers;
using Main.Infrastructure.AI.Helpers.Interfaces;
using Main.Infrastructure.AI.Plugins;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using OpenAI;
using OpenAI.Chat;

using StackExchange.Redis;

namespace Main.Infrastructure.AI;

internal sealed class NativeChatCompletionService(
    OpenAIClient openAiClient,
    IModelRegistry modelRegistry,
    IStreamPublisher streamPublisher,
    IChatLockService chatLockService,
    IUserPreferenceResolver userPreferenceResolver,
    IChatHistoryBuilder chatHistoryBuilder,
    IStreamFinalizer streamFinalizer,
    Kernel kernel,
    PluginUserContext pluginUserContext,
    PluginStreamContext pluginStreamContext,
    ILogger<NativeChatCompletionService> logger
) : INativeChatCompletionService
{
    private static readonly TimeSpan StreamExpiration = TimeSpan.FromHours(1);

    private static readonly JsonSerializerOptions MemoriesJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

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

            TokenUsage tokenUsage;

            if (userId is not null)
            {
                pluginUserContext.UserId = userId.Value;

                tokenUsage = await StreamWithToolsAsync
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
                tokenUsage = await StreamSimpleAsync
                (
                    streamId: streamId,
                    modelId: modelId,
                    messages: messages,
                    messageContent: messageContent,
                    cancellationToken: cancellationToken
                );
            }

            await streamFinalizer.FinalizeAsync
            (
                streamId: streamId,
                chatId: chatId,
                modelId: modelId,
                messageContent: messageContent,
                tokenUsage: tokenUsage,
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

    private async Task<TokenUsage> StreamWithToolsAsync
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
        bool memoryEnabled = await userPreferenceResolver.IsMemoryEnabledAsync(userId, cancellationToken);
        bool memoryToolsEnabled = supportsFunctionCalling && memoryEnabled;
        bool webSearchToolEnabled = supportsFunctionCalling && webSearchEnabled;

        ChatHistoryResult chatHistoryResult = await chatHistoryBuilder.BuildAsync
        (
            messages: messages,
            userId: userId,
            modelInfo: modelInfo,
            memoryToolsEnabled: memoryToolsEnabled,
            webSearchToolEnabled: webSearchToolEnabled,
            cancellationToken: cancellationToken
        );

        ChatHistory chatHistory = chatHistoryResult.ChatHistory;
        IReadOnlyList<MemoryEntry> memories = chatHistoryResult.MemoryEntries;

        if (memories.Count > 0)
        {
            string memoriesJson = JsonSerializer.Serialize
            (
                memories.Select(me => new { me.Content, me.MemoryCategory }),
                MemoriesJsonOptions
            );

            await streamPublisher.PublishMemoriesAsync
            (
                streamId: streamId,
                memoriesJson: memoriesJson,
                cancellationToken: cancellationToken
            );
        }

        string openRouterId = modelRegistry.GetOpenRouterModelId(modelId);

        pluginStreamContext.StreamId = streamId;

        List<KernelFunction> functions = supportsFunctionCalling
            ? kernel.Plugins
                .GetFunctionsMetadata()
                .Where(f =>
                    (webSearchToolEnabled || f.PluginName != "search") &&
                    (memoryToolsEnabled || f.PluginName != "memory"))
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

        TokenUsage tokenUsage = TokenUsage.Empty;

#pragma warning disable S3267
        await foreach (StreamingChatMessageContent chunk in chatService.GetStreamingChatMessageContentsAsync(
                           chatHistory: chatHistory, executionSettings: settings, kernel: kernel,
                           cancellationToken: cancellationToken))
#pragma warning restore S3267
        {
            if (chunk.Metadata?.TryGetValue("Usage", out object? usageObj) == true &&
                usageObj is ChatTokenUsage chatTokenUsage)
                tokenUsage = new TokenUsage
                (
                    InputTokens: chatTokenUsage.InputTokenCount,
                    OutputTokens: chatTokenUsage.OutputTokenCount,
                    TotalTokens: chatTokenUsage.TotalTokenCount
                );

            if (string.IsNullOrWhiteSpace(chunk.Content))
                continue;

            messageContent.Append(chunk.Content);
            await streamPublisher.PublishChunkAsync(streamId, chunk.Content, cancellationToken);
        }

        if (tokenUsage == TokenUsage.Empty && messageContent.Length > 0)
        {
            int estimatedOutput = messageContent.Length / 4;
            tokenUsage = new TokenUsage
            (
                InputTokens: 0,
                OutputTokens: estimatedOutput,
                TotalTokens: estimatedOutput
            );

            logger.LogWarning(
                "Token usage not available from Semantic Kernel streaming for model {ModelId}. Using character-based estimate.",
                modelId);
        }

        return tokenUsage;
    }

    private async Task<TokenUsage> StreamSimpleAsync
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

        TokenUsage tokenUsage = TokenUsage.Empty;

        await foreach (StreamingChatCompletionUpdate update in chatClient.CompleteChatStreamingAsync(chatMessages,
                           cancellationToken: cancellationToken))
        {
            if (update.Usage is { } u)
                tokenUsage = new TokenUsage
                (
                    InputTokens: u.InputTokenCount,
                    OutputTokens: u.OutputTokenCount,
                    TotalTokens: u.TotalTokenCount
                );

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

        return tokenUsage;
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