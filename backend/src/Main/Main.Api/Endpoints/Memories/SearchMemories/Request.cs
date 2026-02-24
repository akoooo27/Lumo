using Main.Application.Abstractions.Memory;

using Microsoft.AspNetCore.Mvc;

namespace Main.Api.Endpoints.Memories.SearchMemories;

internal sealed record Request
{
    [FromQuery]
    public string Query { get; init; } = string.Empty;

    [FromQuery]
    public int Limit { get; init; } = MemoryConstants.DefaultPageSize;
}