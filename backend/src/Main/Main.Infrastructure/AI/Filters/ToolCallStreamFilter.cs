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
        ["__sm"] = "save_memory",
        ["__um"] = "update_memory",
        ["__dm"] = "delete_memory",
        ["__fm"] = "find_memories"
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

            await streamPublisher.PublishToolCallAsync
            (
                streamId: pluginStreamContext.StreamId,
                toolName: displayName,
                query: query,
                cancellationToken: context.CancellationToken
            );

            await next(context);

            if (pluginStreamContext.LastSearchSources is { Count: > 0 } sources)
            {
                string sourcesJson = JsonSerializer.Serialize(
                    sources.Select(s => new { title = s.Title, url = s.Url }));

                await streamPublisher.PublishToolCallResultAsync
                (
                    streamId: pluginStreamContext.StreamId,
                    toolName: displayName,
                    sourcesJson: sourcesJson,
                    cancellationToken: context.CancellationToken
                );

                pluginStreamContext.LastSearchSources = null;
            }

            return;
        }

        await next(context);
    }
}