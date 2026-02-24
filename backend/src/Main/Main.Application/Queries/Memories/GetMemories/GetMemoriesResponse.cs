namespace Main.Application.Queries.Memories.GetMemories;

public sealed record GetMemoriesResponse
(
    IReadOnlyList<MemoryItemReadModel> Memories,
    PaginationInfo PaginationInfo
);

public sealed record PaginationInfo
(
    DateTimeOffset? NextCursor,
    bool HasMore,
    int Limit
);