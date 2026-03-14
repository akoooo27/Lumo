namespace Main.Api.Endpoints.Chats.GetChats;

internal sealed record ChatDto
(
    string Id,
    string Title,
    string? ModelName,
    string? FolderId,
    bool IsArchived,
    bool IsPinned,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    int MessagesCount
);

internal sealed record PaginationDto
(
    DateTimeOffset? NextCursor,
    bool HasMore,
    int Limit
);

internal sealed record Response
(
    IReadOnlyList<ChatDto> Chats,
    PaginationDto Pagination
);