namespace Main.Application.Queries.Memories.SearchMemories;

public sealed record SearchMemoriesResponse
(
    IReadOnlyList<SearchMemoryReadModel> Memories
);