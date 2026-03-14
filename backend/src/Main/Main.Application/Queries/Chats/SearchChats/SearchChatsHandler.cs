using System.Data.Common;

using Dapper;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Chats.SearchChats;

internal sealed class SearchChatsHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<SearchChatsQuery, SearchChatsResponse>
{
    private const string SearchChatsSql = """
                                          WITH search_query AS (
                                              SELECT plainto_tsquery('english', @Query) AS q
                                          ),
                                          message_matches AS (
                                              SELECT DISTINCT ON (c.id)
                                                  c.id AS ChatId,
                                                  c.title AS Title,
                                                  c.model_id AS ModelName,
                                                  c.folder_id AS Folder,
                                                  ts_headline('english', m.message_content, sq.q,
                                                      'MaxWords=30, MinWords=15, StartSel=**, StopSel=**'
                                                  ) AS Snippet,
                                                  COALESCE(c.updated_at, c.created_at) AS MatchedAt,
                                                  c.created_at AS CreatedAt,
                                                  ts_rank(m.search_vector, sq.q) AS rank
                                              FROM messages m
                                              INNER JOIN chats c ON c.id = m.chat_id
                                              CROSS JOIN search_query sq
                                              WHERE c.user_id = @UserId
                                                AND m.search_vector @@ sq.q
                                                AND (@Cursor IS NULL OR COALESCE(c.updated_at, c.created_at) < @Cursor)
                                              ORDER BY c.id, rank DESC
                                          ),
                                          title_matches AS (
                                              SELECT
                                                  c.id AS ChatId,
                                                  c.title AS Title,
                                                  c.model_id AS ModelName,
                                                  c.folder_id AS Folder,
                                                  ts_headline('english', c.title, sq.q,
                                                      'StartSel=**, StopSel=**'
                                                  ) AS Snippet,
                                                  COALESCE(c.updated_at, c.created_at) AS MatchedAt,
                                                  c.created_at AS CreatedAt,
                                                  ts_rank(c.title_search_vector, sq.q) AS rank
                                              FROM chats c
                                              CROSS JOIN search_query sq
                                              WHERE c.user_id = @UserId
                                                AND c.title_search_vector @@ sq.q
                                                AND (@Cursor IS NULL OR COALESCE(c.updated_at, c.created_at) < @Cursor)
                                          ),
                                          combined AS (
                                              SELECT * FROM message_matches
                                              UNION ALL
                                              SELECT * FROM title_matches
                                          ),
                                          deduplicated AS (
                                              SELECT DISTINCT ON (ChatId)
                                                  ChatId, Title, ModelName, Folder, Snippet, MatchedAt, CreatedAt, rank
                                              FROM combined
                                              ORDER BY ChatId, rank DESC
                                          )
                                          SELECT ChatId, Title, ModelName, Folder, Snippet, MatchedAt, CreatedAt
                                          FROM deduplicated
                                          ORDER BY MatchedAt DESC
                                          LIMIT @FetchLimit
                                          """;

    public async ValueTask<Outcome<SearchChatsResponse>> Handle(SearchChatsQuery request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        int fetchLimit = request.Limit + 1;

        IEnumerable<SearchChatReadModel> results = await connection.QueryAsync<SearchChatReadModel>
        (
            SearchChatsSql,
            new { UserId = userId, Query = request.Query, Cursor = request.Cursor, FetchLimit = fetchLimit }
        );

        List<SearchChatReadModel> resultList = results.AsList();

        bool hasMore = resultList.Count > request.Limit;

        if (hasMore)
            resultList.RemoveAt(resultList.Count - 1);

        DateTimeOffset? nextCursor = hasMore
            ? resultList[^1].MatchedAt
            : null;

        SearchPaginationInfo paginationInfo = new
        (
            NextCursor: nextCursor,
            HasMore: hasMore,
            Limit: request.Limit
        );

        SearchChatsResponse response = new
        (
            Results: resultList,
            PaginationInfo: paginationInfo
        );

        return response;
    }
}