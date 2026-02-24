using Main.Application.Abstractions.Memory;

using Pgvector;

namespace Main.Infrastructure.Data.Entities;

internal sealed class MemoryRecord
{
    public required string Id { get; init; }

    public required Guid UserId { get; init; }

    public required string Content { get; set; }

    public required MemoryCategory Category { get; set; }

    public required Vector Embedding { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset LastAccessedAt { get; set; }

    public int AccessCount { get; set; }

    public int Importance { get; set; } // 0-10

    public bool IsActive { get; set; } = true;
}