namespace Main.Api.Endpoints.Memories.SearchMemories;

internal sealed record MemoryDto
(
    string Id,
    string Content,
    string Category,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset LastAccessedAt,
    int AccessCount,
    int Importance
);

internal sealed record Response
(
    IReadOnlyList<MemoryDto> Memories
);