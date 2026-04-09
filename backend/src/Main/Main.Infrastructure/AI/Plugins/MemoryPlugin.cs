using System.ClientModel;
using System.ComponentModel;

using Main.Application.Abstractions.Memory;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Main.Infrastructure.AI.Plugins;

internal sealed class MemoryPlugin
(
    PluginUserContext userContext,
    IMemoryStore memoryStore,
    ILogger<MemoryPlugin> logger
)
{
    [KernelFunction("save")]
    [Description(
        "Save important information about the user to memory for future conversations. " +
        "Before saving, always search existing memories first to avoid duplicates. " +
        "If a similar memory exists, update it instead of creating a new one. " +
        "Use when the user shares preferences, personal facts, or instructions they want remembered. " +
        "Examples: 'I prefer dark mode', 'My name is John', 'Always respond in Spanish'.")]
    public async Task<string> SaveMemoryAsync
    (
        [Description("The specific information to remember about the user. Be concise but complete.")]
        string content,
        [Description("The type of memory: 'preference' for user preferences, 'fact' for personal information, 'instruction' for behavioral guidelines.")]
        MemoryCategory category,
        [Description("How important this memory is from 1-10. Use 8-10 for core identity/critical preferences, 5-7 for useful context, 1-4 for minor details.")]
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
        "Update an existing memory when information changes. " +
        "Use when the user corrects or updates previously saved information. " +
        "Example: user previously said 'I work at Google' but now says 'I switched to Microsoft'. " +
        "You must provide the memory ID from a previous search result.")]
    public async Task<string> UpdateMemoryAsync
    (
        [Description("The ID of the memory to update (from search results).")]
        string memoryId,
        [Description("The updated content for this memory. Leave null if only updating category/importance.")]
        string? newContent = null,
        [Description("Optional new category: 'preference', 'fact', or 'instruction'.")]
        MemoryCategory? newCategory = null,
        [Description("Optional new importance from 1-10.")]
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
        "Delete a memory that is no longer accurate or relevant. " +
        "Use when the user explicitly asks you to forget something, " +
        "or when information is clearly outdated and no update makes sense. " +
        "You must provide the memory ID from a previous search result.")]
    public async Task<string> DeleteMemoryAsync
    (
        [Description("The ID of the memory to delete (from search results).")]
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
        "Search your stored memories about this user. " +
        "Use before saving a new memory to check for duplicates. " +
        "Use when you need to recall specific stored information or find a memory ID for update/delete.")]
    public async Task<string> FindMemoriesAsync
    (
        [Description("What to search for in memories. Be specific.")]
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
        "Recall stored memories about this user that are relevant to the current conversation. " +
        "Use when you need personalized context — the user's name, preferences, past topics, " +
        "or when the user references something from a previous conversation or asks 'do you remember'. " +
        "Do NOT use for general knowledge questions like math, coding, or facts.")]
    public async Task<string> RecallMemoriesAsync
    (
        [Description(
            "Describe what you need to know about the user. Be specific — e.g. 'programming language preferences' rather than 'everything'.")]
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