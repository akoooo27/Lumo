using Main.Application.Abstractions.Memory;

using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Memories.GetMemories;

public sealed record GetMemoriesQuery
(
    MemoryCategory? Category,
    DateTimeOffset? Cursor,
    int Limit
) : IQuery<GetMemoriesResponse>;