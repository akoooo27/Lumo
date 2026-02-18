namespace Main.Application.Abstractions.Memory;

public sealed record MemoryEntry
(
    string Id,
    string Content,
    MemoryCategory MemoryCategory,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset LastAccessedAt,
    int AccessCount,
    int Importance
);