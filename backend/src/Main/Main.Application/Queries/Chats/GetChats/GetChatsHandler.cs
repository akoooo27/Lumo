using System.Data.Common;

using Dapper;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Chats.GetChats;

internal sealed class GetChatsHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetChatsQuery, GetChatsResponse>
{
    private const string GetChatsSql = """
                                       SELECT
                                           id as Id,
                                           title as Title,
                                           model_id as ModelName,
                                           is_archived as IsArchived,
                                           is_pinned as IsPinned,
                                           folder_id as FolderId,
                                           created_at as CreatedAt,
                                           updated_at as UpdatedAt,
                                           next_sequence_number as MessagesCount
                                       FROM chats
                                       WHERE user_id = @UserId
                                         AND (@Cursor IS NULL OR COALESCE(updated_at, created_at) < @Cursor)
                                         AND (@HasFolderFilter = FALSE OR (@FolderIsNull = TRUE AND folder_id IS NULL) OR folder_id = @FolderId)
                                       ORDER BY COALESCE(updated_at, created_at) DESC
                                       LIMIT @FetchLimit
                                       """;

    public async ValueTask<Outcome<GetChatsResponse>> Handle(GetChatsQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        int fetchLimit = request.Limit + 1;

        IEnumerable<ChatReadModel> chats = await connection.QueryAsync<ChatReadModel>(
            GetChatsSql,
            new
            {
                UserId = userContext.UserId,
                Cursor = request.Cursor,
                FetchLimit = fetchLimit,
                HasFolderFilter = request.HasFolderId,
                FolderIsNull = request.HasFolderId && string.IsNullOrEmpty(request.FolderId),
                FolderId = request.FolderId
            });

        List<ChatReadModel> chatList = chats.AsList();

        bool hasMore = chatList.Count > request.Limit;

        if (hasMore)
            chatList.RemoveAt(chatList.Count - 1);

        DateTimeOffset? nextCursor = hasMore
            ? chatList[^1].UpdatedAt ?? chatList[^1].CreatedAt
            : null;

        PaginationInfo paginationInfo = new
        (
            NextCursor: nextCursor,
            HasMore: hasMore,
            Limit: request.Limit
        );

        GetChatsResponse response = new
        (
            Chats: chatList,
            PaginationInfo: paginationInfo
        );

        return response;
    }
}