namespace Main.Application.Queries.Memories.SearchMemories;

public sealed record SearchMemoryReadModel
{
    public required string Id { get; init; }

    public required string Content { get; init; }

    public required string Category { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public DateTimeOffset LastAccessedAt { get; init; }

    public required int AccessCount { get; init; }

    public required int Importance { get; init; }
}