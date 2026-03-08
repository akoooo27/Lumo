using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Chats.SearchChats;

public sealed record SearchChatsQuery
(
    string Query,
    DateTimeOffset? Cursor,
    int Limit
) : IQuery<SearchChatsResponse>;