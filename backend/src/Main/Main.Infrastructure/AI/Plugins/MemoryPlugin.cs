using System.ClientModel;
using System.ComponentModel;

using Main.Application.Abstractions.Memory;
using Main.Infrastructure.AI.Models;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Main.Infrastructure.AI.Plugins;

internal sealed class MemoryPlugin
(
    PluginUserContext userContext,
    PluginStreamContext pluginStreamContext,
    IMemoryStore memoryStore,
    ILogger<MemoryPlugin> logger
)
{
    [KernelFunction("save")]
    [Description(
        "Save a user fact, preference, or instruction to memory. " +
        "Search first to avoid duplicates; update existing entries when possible.")]
    public async Task<string> SaveMemoryAsync
    (
        [Description("Concise information to remember about the user.")]
        string content,
        [Description("'preference', 'fact', or 'instruction'.")]
        MemoryCategory category,
        [Description("1-10. High (8-10): core identity. Medium (5-7): useful context. Low (1-4): minor details.")]
        int importance,
        CancellationToken cancellationToken
    )
    {
        Guid userId = userContext.UserId;

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "SaveMemoryAsync called for user {UserId}, content length: {ContentLength}, category: {Category}",
                userId, content.Length, category);

        try
        {
            string memoryId = await memoryStore.SaveAsync
            (
                userId: userId,
                content: content,
                memoryCategory: category,
                importance: importance,
                cancellationToken: cancellationToken
            );

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Memory saved successfully: {MemoryId} for user {UserId}", memoryId, userId);
            return $"Memory saved with ID: {memoryId}";
        }
        catch (ArgumentException exception)
        {
            logger.LogWarning(exception, "Invalid memory content from AI");
            return $"Failed to save memory: {exception.Message}";
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Memory limit reached for user {UserId}", userId);
            return $"Failed to save memory: {exception.Message}";
        }
    }

    [KernelFunction("update")]
    [Description(
        "Update an existing memory by ID (from find results) when information changes.")]
    public async Task<string> UpdateMemoryAsync
    (
        [Description("Memory ID from find results.")]
        string memoryId,
        [Description("New content, or null to keep existing.")]
        string? newContent = null,
        [Description("New category, or null to keep existing.")]
        MemoryCategory? newCategory = null,
        [Description("New importance 1-10, or null to keep existing.")]
        int? newImportance = null,
        CancellationToken cancellationToken = default
    )
    {
        Guid userId = userContext.UserId;

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "UpdateMemoryAsync called for user {UserId}, memoryId: {MemoryId}",
                userId, memoryId);

        if (newContent is null && newCategory is null && newImportance is null)
            return "Failed to update memory: provide at least one field (content, category, or importance).";

        if (newContent is not null && string.IsNullOrWhiteSpace(newContent))
            return "Failed to update memory: content cannot be empty.";

        try
        {
            await memoryStore.UpdateAsync
            (
                userId: userId,
                memoryId: memoryId,
                newContent: newContent,
                newCategory: newCategory,
                newImportance: newImportance,
                cancellationToken: cancellationToken
            );

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Memory updated: {MemoryId} for user {UserId}", memoryId, userId);

            return $"Memory {memoryId} updated successfully.";
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Memory not found for update: {MemoryId}", memoryId);
            return $"Failed to update memory: {exception.Message}";
        }
    }

    [KernelFunction("delete")]
    [Description(
        "Delete a memory by ID (from find results) when it is outdated or the user asks to forget.")]
    public async Task<string> DeleteMemoryAsync
    (
        [Description("Memory ID from find results.")]
        string memoryId,
        CancellationToken cancellationToken
    )
    {
        Guid userId = userContext.UserId;

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation
            (
                "DeleteMemoryAsync called for user {UserId}, memoryId: {MemoryId}",
                userId, memoryId
            );

        try
        {
            await memoryStore.SoftDeleteAsync
            (
                userId: userId,
                memoryId: memoryId,
                cancellationToken: cancellationToken
            );

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Memory deleted: {MemoryId} for user {UserId}", memoryId, userId);

            return $"Memory {memoryId} deleted successfully.";
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Memory not found for delete: {MemoryId}", memoryId);
            return $"Failed to delete memory: {exception.Message}";
        }
    }

    [KernelFunction("find")]
    [Description(
        "Search stored memories. Use to check for duplicates before saving or to get a memory ID for update/delete.")]
    public async Task<string> FindMemoriesAsync
    (
        [Description("Specific search query.")]
        string query,
        CancellationToken cancellationToken
    )
    {
        Guid userId = userContext.UserId;

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "FindMemoriesAsync called for user {UserId}, query length: {QueryLength}",
                userId, query.Length);

        try
        {
            IReadOnlyList<MemoryEntry> memories = await memoryStore.SearchAsync
            (
                userId: userId,
                query: query,
                limit: MemoryConstants.MaxMemorySearchResults,
                cancellationToken: cancellationToken
            );

            if (memories.Count == 0)
                return "No matching memories found.";

            return string.Join("\n", memories.Select(m =>
                $"[{m.Id}] [{m.MemoryCategory}] [importance:{m.Importance}] {m.Content}"));
        }
        catch (ClientResultException exception)
        {
            logger.LogWarning(exception, "Failed to search memories for user {UserId} (API error)", userId);
            return "Failed to search memories. You may save a new memory instead.";
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to search memories for user {UserId} (network error)", userId);
            return "Failed to search memories. You may save a new memory instead.";
        }
    }

    [KernelFunction("recall")]
    [Description(
        "Retrieve memories relevant to the current conversation for personalization. " +
        "Use when the user's name, preferences, or past topics would help. Not for general knowledge.")]
    public async Task<string> RecallMemoriesAsync
    (
        [Description("What you need to know about the user — be specific.")]
        string context,
        CancellationToken cancellationToken
    )
    {
        Guid userId = userContext.UserId;

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "RecallMemoriesAsync called for user {UserId}, context length: {ContextLength}",
                userId, context.Length);

        try
        {
            IReadOnlyList<MemoryEntry> memories = await memoryStore.GetRelevantAsync
            (
                userId: userId,
                context: context,
                limit: MemoryConstants.MaxMemoriesInContext,
                cancellationToken: cancellationToken
            );

            if (memories.Count == 0)
                return "No relevant memories found for this user.";

            pluginStreamContext.RecalledMemories =
            [
                .. memories.Select(m => new RecalledMemory
                (
                    Content: m.Content,
                    MemoryCategory: m.MemoryCategory.ToString()
                ))
            ];

            return string.Join("\n", memories.Select(m =>
                $"[{m.MemoryCategory}] {m.Content}"));
        }
        catch (ClientResultException exception)
        {
            logger.LogWarning(exception, "Failed to recall memories for user {UserId} (API error)", userId);
            return "Memory recall is temporarily unavailable.";
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to recall memories for user {UserId} (network error)", userId);
            return "Memory recall is temporarily unavailable.";
        }
    }
}