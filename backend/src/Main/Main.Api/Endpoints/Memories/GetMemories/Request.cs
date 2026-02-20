using Main.Application.Abstractions.Memory;

using Microsoft.AspNetCore.Mvc;

namespace Main.Api.Endpoints.Memories.GetMemories;

internal sealed record Request
{
    [FromQuery]
    public MemoryCategory? Category { get; init; }

    [FromQuery]
    public DateTimeOffset? Cursor { get; init; }

    [FromQuery]
    public int Limit { get; init; } = MemoryConstants.DefaultPageSize;
}