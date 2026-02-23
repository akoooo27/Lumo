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
    public async Task<string> SaveAsync(Guid userId, string content, MemoryCategory memoryCategory, int importance,
        CancellationToken cancellationToken)
    {
        int clampedImportance = Math.Clamp(importance, MemoryConstants.MinImportance, MemoryConstants.MaxImportance);

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

    public async Task UpdateAsync(Guid userId, string memoryId, string newContent, CancellationToken cancellationToken)
    {
        MemoryRecord? memoryRecord = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Id == memoryId && m.UserId == userId && m.IsActive, cancellationToken);

        if (memoryRecord is null)
            throw new InvalidOperationException("Memory not found.");

        float[] newEmbedding = await GenerateEmbeddingAsync(newContent, cancellationToken);

        memoryRecord.Content = newContent;
        memoryRecord.Embedding = new Vector(newEmbedding);
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

        List<MemoryRecord> searchResults = await SearchByVectorAsync(userId, new Vector(queryEmbedding), limit, cancellationToken);

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

        List<MemoryRecord> relevantRecords = await SearchByVectorAsync
        (
            userId: userId,
            queryVector: queryVector,
            limit: limit,
            cancellationToken: cancellationToken
        );

        return [.. relevantRecords.Select(ToEntry)];
    }

    private async Task<List<MemoryRecord>> SearchByVectorAsync(Guid userId, Vector queryVector, int limit, CancellationToken cancellationToken)
    {
        List<MemoryRecord> memoryRecords = await dbContext.Memories
            .Where(m => m.UserId == userId && m.IsActive)
            .OrderBy(m => m.Embedding.CosineDistance(queryVector))
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (memoryRecords.Count > 0)
        {
            List<string> ids = memoryRecords.Select(m => m.Id).ToList();

            await dbContext.Memories
                .Where(m => ids.Contains(m.Id))
                .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.LastAccessedAt, dateTimeProvider.UtcNow)
                        .SetProperty(m => m.AccessCount, m => m.AccessCount + 1),
                    cancellationToken);
        }

        return memoryRecords;
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