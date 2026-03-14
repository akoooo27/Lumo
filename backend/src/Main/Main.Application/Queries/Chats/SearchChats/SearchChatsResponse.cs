namespace Main.Application.Queries.Chats.SearchChats;

public sealed record SearchChatsResponse
(
    IReadOnlyList<SearchChatReadModel> Results,
    SearchPaginationInfo PaginationInfo
);