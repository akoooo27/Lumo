namespace Main.Api.Endpoints.Memories.GetMemories;

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

internal sealed record PaginationDto
(
    DateTimeOffset? NextCursor,
    bool HasMore,
    int Limit
);

internal sealed record Response
(
    IReadOnlyList<MemoryDto> Memories,
    PaginationDto Pagination
);