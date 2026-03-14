using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Chats.GetChats;

public sealed record GetChatsQuery
(
    DateTimeOffset? Cursor,
    int Limit,
    string? FolderId,
    bool HasFolderId
) : IQuery<GetChatsResponse>;