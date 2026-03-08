namespace Main.Application.Queries.Chats.SearchChats;

public sealed record SearchPaginationInfo
(
    DateTimeOffset? NextCursor,
    bool HasMore,
    int Limit
);