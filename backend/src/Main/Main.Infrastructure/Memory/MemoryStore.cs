using System.ClientModel;

using Main.Application.Abstractions.Memory;
using Main.Infrastructure.Data;
using Main.Infrastructure.Data.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using OpenAI.Embeddings;

using Pgvector;
using Pgvector.EntityFrameworkCore;

using SharedKernel;

namespace Main.Infrastructure.Memory;

internal sealed class MemoryStore(
    MainDbContext dbContext,
    EmbeddingClient embeddingClient,
    IDateTimeProvider dateTimeProvider,
    ILogger<MemoryStore> logger) : IMemoryStore
{
    private const float ImportanceRetrievalWeight = 0.03f;

    public async Task<string> SaveAsync(Guid userId, string content, MemoryCategory memoryCategory, int importance,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Memory content cannot be empty.", nameof(content));

        if (content.Length > MemoryConstants.MaxContentLength)
            throw new ArgumentException(
                $"Memory content exceeds maximum length of {MemoryConstants.MaxContentLength} characters.",
                nameof(content));

        int clampedImportance = Math.Clamp(importance, MemoryConstants.MinImportance, MemoryConstants.MaxImportance);

        int currentCount = await dbContext.Memories
            .Where(m => m.UserId == userId && m.IsActive)
            .CountAsync(cancellationToken);

        if (currentCount >= MemoryConstants.MaxMemoriesPerUser)
        {
            MemoryRecord? evictionTarget = await dbContext.Memories
                .Where(m => m.UserId == userId && m.IsActive)
                .OrderBy(m => m.Importance)
                .ThenBy(m => m.LastAccessedAt)
                .ThenBy(m => m.AccessCount)
                .FirstOrDefaultAsync(cancellationToken);

            if (evictionTarget is not null)
            {
                evictionTarget.IsActive = false;
                evictionTarget.UpdatedAt = dateTimeProvider.UtcNow;

                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation(
                        "Evicted memory {MemoryId} (importance: {Importance}, accessCount: {AccessCount}) for user {UserId} to stay within limit",
                        evictionTarget.Id, evictionTarget.Importance, evictionTarget.AccessCount, userId);
            }


        }

        float[] embedding = await GenerateEmbeddingAsync(content, cancellationToken);

        MemoryRecord memoryRecord = new()
        {
            Id = $"mem_{Ulid.NewUlid()}",
            UserId = userId,
            Content = content,
            Category = memoryCategory,
            Embedding = new Vector(embedding),
            CreatedAt = dateTimeProvider.UtcNow,
            LastAccessedAt = dateTimeProvider.UtcNow,
            AccessCount = 0,
            Importance = clampedImportance,
            IsActive = true,
        };

        await dbContext.Memories.AddAsync(memoryRecord, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return memoryRecord.Id;
    }

    public async Task UpdateAsync(Guid userId, string memoryId, string? newContent, MemoryCategory? newCategory,
        int? newImportance, CancellationToken cancellationToken)
    {
        MemoryRecord? memoryRecord = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Id == memoryId && m.UserId == userId && m.IsActive, cancellationToken);

        if (memoryRecord is null)
            throw new InvalidOperationException("Memory not found.");

        if (newContent is not null)
        {
            float[] newEmbedding = await GenerateEmbeddingAsync(newContent, cancellationToken);

            memoryRecord.Content = newContent;
            memoryRecord.Embedding = new Vector(newEmbedding);
        }

        if (newCategory is not null)
            memoryRecord.Category = newCategory.Value;

        if (newImportance is not null)
            memoryRecord.Importance = Math.Clamp(newImportance.Value,
                MemoryConstants.MinImportance, MemoryConstants.MaxImportance);

        memoryRecord.UpdatedAt = dateTimeProvider.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryEntry>> SearchAsync(Guid userId, string query, int limit, CancellationToken cancellationToken)
    {
        float[] queryEmbedding;

        try
        {
            queryEmbedding = await GenerateEmbeddingAsync(query, cancellationToken);
        }
        catch (ClientResultException exception)
        {
            logger.LogWarning(exception, "Failed to generate query embedding (API error)");
            return [];
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to generate query embedding (network error)");
            return [];
        }

        Vector queryVector = new(queryEmbedding);

        List<MemoryRecord> searchResults = await dbContext.Memories
            .Where(m => m.UserId == userId && m.IsActive)
            .OrderBy(m => m.Embedding.CosineDistance(queryVector))
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (searchResults.Count > 0)
        {
            List<string> ids = [.. searchResults.Select(m => m.Id)];

            await dbContext.Memories
                .Where(m => ids.Contains(m.Id))
                .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.LastAccessedAt, dateTimeProvider.UtcNow)
                        .SetProperty(m => m.AccessCount, m => m.AccessCount + 1),
                    cancellationToken);
        }

        return [.. searchResults.Select(ToEntry)];
    }

    public async Task<IReadOnlyList<MemoryEntry>> GetRelevantAsync(Guid userId, string context, int limit, CancellationToken cancellationToken)
    {
        float[] queryEmbedding;

        try
        {
            queryEmbedding = await GenerateEmbeddingAsync(context, cancellationToken);
        }
        catch (ClientResultException exception)
        {
            logger.LogWarning(exception, "Failed to generate query embedding (API error), falling back to recent memories");
            return await GetRecentAsync(userId, limit, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to generate query embedding (network error), falling back to recent memories");
            return await GetRecentAsync(userId, limit, cancellationToken);
        }

        Vector queryVector = new(queryEmbedding);
        int fetchCount = limit * 2;

        var candidates = await dbContext.Memories
            .Where(m => m.UserId == userId && m.IsActive)
            .OrderBy(m => m.Embedding.CosineDistance(queryVector))
            .Take(fetchCount)
            .Select(m => new
            {
                Record = m,
                Distance = m.Embedding.CosineDistance(queryVector)
            })
            .ToListAsync(cancellationToken);

        // Lower the distance more important it becomes.
        // This will account for the importance so that very important memories get pushed up list
        // Even if they are not as close to the query.
        List<MemoryRecord> reranked = [.. candidates
            .OrderBy(c => c.Distance - c.Record.Importance * ImportanceRetrievalWeight)
            .Take(limit)
            .Select(c => c.Record)];

        if (reranked.Count > 0)
        {
            List<string> ids = [.. reranked.Select(m => m.Id)];

            await dbContext.Memories
                .Where(m => ids.Contains(m.Id))
                .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.LastAccessedAt, dateTimeProvider.UtcNow)
                        .SetProperty(m => m.AccessCount, m => m.AccessCount + 1),
                    cancellationToken);
        }

        return [.. reranked.Select(ToEntry)];
    }

    public async Task<IReadOnlyList<MemoryEntry>> GetRecentAsync(Guid userId, int limit, CancellationToken cancellationToken)
    {
        List<MemoryRecord> records = await dbContext.Memories
            .Where(m => m.UserId == userId && m.IsActive)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return [.. records.Select(ToEntry)];
    }

    public async Task<MemoryEntry?> GetByIdAsync(Guid userId, string memoryId, CancellationToken cancellationToken)
    {
        MemoryRecord? memoryRecord = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Id == memoryId && m.UserId == userId && m.IsActive, cancellationToken);

        return memoryRecord is null
            ? null
            : ToEntry(memoryRecord);
    }

    public async Task<int> GetCountAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Memories
            .Where(m => m.UserId == userId && m.IsActive)
            .CountAsync(cancellationToken);

    public async Task SoftDeleteAsync(Guid userId, string memoryId, CancellationToken cancellationToken)
    {
        MemoryRecord? memoryRecord = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Id == memoryId && m.UserId == userId, cancellationToken);

        if (memoryRecord is null)
            return;

        memoryRecord.IsActive = false;
        memoryRecord.UpdatedAt = dateTimeProvider.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        await dbContext.Memories
            .Where(m => m.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> BulkSaveAsync(Guid userId, IReadOnlyList<ImportEntry> entries, CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
            return [];

        List<string> entryContents = [.. entries.Select(ie => ie.Content)];

        OpenAIEmbeddingCollection embeddingCollection =
            await embeddingClient.GenerateEmbeddingsAsync(entryContents, cancellationToken: cancellationToken);

        float[][] embeddingArrays = embeddingCollection
            .OrderBy(e => e.Index)
            .Select(e => e.ToFloats().ToArray())
            .ToArray();

        DateTimeOffset now = dateTimeProvider.UtcNow;
        List<string> ids = [];

        for (int i = 0; i < entries.Count; i++)
        {
            string id = $"mem_{Ulid.NewUlid()}";

            MemoryRecord memoryRecord = new()
            {
                Id = id,
                UserId = userId,
                Content = entries[i].Content,
                Category = entries[i].Category,
                Embedding = new(embeddingArrays[i]),
                CreatedAt = now,
                LastAccessedAt = now,
                AccessCount = 0,
                Importance = entries[i].Importance,
                IsActive = true,
            };

            await dbContext.Memories.AddAsync(memoryRecord, cancellationToken);
            ids.Add(id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Bulk imported {Count} memories for user {UserId}", ids.Count, userId);

        return ids;
    }

    private static MemoryEntry ToEntry(MemoryRecord memoryRecord) =>
        new
        (
            Id: memoryRecord.Id,
            Content: memoryRecord.Content,
            MemoryCategory: memoryRecord.Category,
            CreatedAt: memoryRecord.CreatedAt,
            UpdatedAt: memoryRecord.UpdatedAt,
            LastAccessedAt: memoryRecord.LastAccessedAt,
            AccessCount: memoryRecord.AccessCount,
            Importance: memoryRecord.Importance
        );


    private async Task<float[]> GenerateEmbeddingAsync(string content, CancellationToken cancellationToken)
    {
        try
        {
            OpenAIEmbedding embedding =
                await embeddingClient.GenerateEmbeddingAsync(content, cancellationToken: cancellationToken);

            return embedding.ToFloats().ToArray();
        }
        catch (ClientResultException exception)
        {
            logger.LogError(exception, "Failed to generate embedding for text");
            throw;
        }
    }
}