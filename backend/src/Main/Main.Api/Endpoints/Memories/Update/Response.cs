namespace Main.Api.Endpoints.Memories.Update;

internal sealed record Response
(
    string MemoryId,
    string Content,
    DateTimeOffset UpdatedAt
);