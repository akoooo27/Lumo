namespace Main.Api.Endpoints.Chats.SearchChats;

internal sealed record SearchResultDto
(
    string ChatId,
    string Title,
    string? ModelName,
    string? Folder,
    string Snippet,
    DateTimeOffset MatchedAt,
    DateTimeOffset CreatedAt
);

internal sealed record PaginationDto
(
    DateTimeOffset? NextCursor,
    bool HasMore,
    int Limit
);

internal sealed record Response
(
    IReadOnlyList<SearchResultDto> Results,
    PaginationDto Pagination
);