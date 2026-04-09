using System.Text.Json;

using Main.Application.Abstractions.Stream;
using Main.Infrastructure.AI.Plugins;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Main.Infrastructure.AI.Filters;

internal sealed class ToolCallStreamFilter(
    PluginStreamContext pluginStreamContext,
    IStreamPublisher streamPublisher,
    ILogger<ToolCallStreamFilter> logger) : IAutoFunctionInvocationFilter
{
    private static readonly Dictionary<string, string> ToolDisplayNames = new()
    {
        ["__ws"] = "web_search",
        ["save"] = "save_memory",
        ["update"] = "update_memory",
        ["delete"] = "delete_memory",
        ["find"] = "find_memories",
        ["recall"] = "recall_memories"
    };

    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        string functionName = context.Function.Name;

        if (pluginStreamContext.StreamId is not null &&
            ToolDisplayNames.TryGetValue(functionName, out var displayName))
        {
            string? query = context.Arguments?.ContainsKey("query") == true
                ? context.Arguments["query"]?.ToString()
                : null;

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "Tool call intercepted: {FunctionName} → streaming as {DisplayName} to stream {StreamId}",
                    functionName, displayName, pluginStreamContext.StreamId);

            await streamPublisher.PublishThinkingAsync
            (
                streamId: pluginStreamContext.StreamId,
                phase: displayName switch
                {
                    "web_search" => "Searching the web...",
                    "save_memory" => "Saving to memory...",
                    "find_memories" => "Searching memories...",
                    "recall_memories" => "Recalling memories...",
                    _ => "Processing..."
                },
                cancellationToken: context.CancellationToken
            );

            await streamPublisher.PublishToolCallAsync
            (
                streamId: pluginStreamContext.StreamId,
                toolName: displayName,
                query: query,
                cancellationToken: context.CancellationToken
            );

            await next(context);

            if (pluginStreamContext.RecalledMemories is { Count: > 0 } recalledMemories)
            {
                string memoriesJson = JsonSerializer.Serialize
                (
                    recalledMemories.Select(m => new { m.Content, m.MemoryCategory })
                );

                await streamPublisher.PublishMemoriesAsync
                (
                    streamId: pluginStreamContext.StreamId,
                    memoriesJson: memoriesJson,
                    cancellationToken: context.CancellationToken
                );

                pluginStreamContext.RecalledMemories = null;
            }

            if (pluginStreamContext.LastSearchSources is { Count: > 0 } sources)
            {
                string sourcesJson = JsonSerializer.Serialize
                (
                    sources.Select(s => new
                    {
                        title = s.Title,
                        url = s.Url,
                        score = s.Score,
                        confidence = s.Confidence,
                        publishedDate = s.PublishedDate
                    })
                );

                await streamPublisher.PublishToolCallResultAsync
                (
                    streamId: pluginStreamContext.StreamId,
                    toolName: displayName,
                    sourcesJson: sourcesJson,
                    cancellationToken: context.CancellationToken
                );

                pluginStreamContext.LastSearchSources = null;
                pluginStreamContext.SourcesJson = sourcesJson;
            }

            return;
        }

        await next(context);
    }
}