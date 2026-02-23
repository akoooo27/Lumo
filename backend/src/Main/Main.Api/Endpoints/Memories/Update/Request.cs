using Main.Application.Abstractions.Memory;

namespace Main.Api.Endpoints.Memories.Update;

internal sealed record Request
(
    string MemoryId,
    string? Content,
    MemoryCategory? Category,
    int? Importance
);