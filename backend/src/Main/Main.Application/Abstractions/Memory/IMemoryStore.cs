namespace Main.Application.Abstractions.Memory;

public interface IMemoryStore
{
    Task<string> SaveAsync(Guid userId, string content, MemoryCategory memoryCategory,
        int importance, CancellationToken cancellationToken);

    Task UpdateAsync(Guid userId, string memoryId, string? newContent,
        MemoryCategory? newCategory, int? newImportance,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MemoryEntry>> SearchAsync(Guid userId, string query, int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MemoryEntry>> GetRelevantAsync(Guid userId, string context, int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MemoryEntry>> GetRecentAsync(Guid userId, int limit,
        CancellationToken cancellationToken);

    Task<MemoryEntry?> GetByIdAsync(Guid userId, string memoryId,
        CancellationToken cancellationToken);

    Task<int> GetCountAsync(Guid userId, CancellationToken cancellationToken);

    Task SoftDeleteAsync(Guid userId, string memoryId, CancellationToken cancellationToken);

    Task DeleteAllAsync(Guid userId, CancellationToken cancellationToken);
}