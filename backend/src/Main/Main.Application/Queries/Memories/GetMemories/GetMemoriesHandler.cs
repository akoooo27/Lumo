using System.Data.Common;

using Dapper;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Memories.GetMemories;

internal sealed class GetMemoriesHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetMemoriesQuery, GetMemoriesResponse>
{
    private const string GetMemoriesSql = """
                                          SELECT
                                            id AS Id,
                                            content AS Content,
                                            category AS Category,
                                            created_at AS CreatedAt,
                                            updated_at AS UpdatedAt,
                                            last_accessed_at AS LastAccessedAt,
                                            access_count AS AccessCount,
                                            importance AS Importance
                                          FROM memories
                                          WHERE user_id = @UserId
                                            AND is_active = true
                                            AND (@Category IS NULL OR category = @Category)
                                            AND (@Cursor IS NULL OR created_at < @Cursor)
                                          ORDER BY created_at DESC
                                          LIMIT @FetchLimit
                                          """;

    public async ValueTask<Outcome<GetMemoriesResponse>> Handle(GetMemoriesQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        int fetchLimit = request.Limit + 1;

        IEnumerable<MemoryItemReadModel> memories = await connection.QueryAsync<MemoryItemReadModel>(
            GetMemoriesSql,
            new
            {
                UserId = userContext.UserId,
                Category = request.Category?.ToString(),
                Cursor = request.Cursor,
                FetchLimit = fetchLimit
            });

        List<MemoryItemReadModel> memoryList = memories.AsList();

        bool hasMore = memoryList.Count > request.Limit;

        if (hasMore)
            memoryList.RemoveAt(memoryList.Count - 1);

        DateTimeOffset? nextCursor = hasMore
            ? memoryList[^1].CreatedAt
            : null;

        PaginationInfo paginationInfo = new
        (
            NextCursor: nextCursor,
            HasMore: hasMore,
            Limit: request.Limit
        );

        GetMemoriesResponse response = new
        (
            Memories: memoryList,
            PaginationInfo: paginationInfo
        );

        return response;
    }
}