namespace Main.Api.Endpoints.Memories.GetById;

internal sealed record Response
(
    string Id,
    string Content,
    string Category,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset LastAccessedAt,
    int AccessCount,
    int Importance
);