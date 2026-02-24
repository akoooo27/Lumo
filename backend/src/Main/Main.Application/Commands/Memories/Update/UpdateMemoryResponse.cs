namespace Main.Application.Commands.Memories.Update;

public sealed record UpdateMemoryResponse
(
    string MemoryId,
    string Content,
    DateTimeOffset UpdatedAt
);