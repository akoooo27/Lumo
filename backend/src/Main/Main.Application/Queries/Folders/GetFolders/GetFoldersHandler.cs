using System.Data.Common;

using Dapper;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Folders.GetFolders;

internal sealed class GetFoldersHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetFoldersQuery, GetFoldersResponse>
{
    private const string GetFoldersSql = """
                                         SELECT
                                             f.id AS FolderId,
                                             f.name AS Name,
                                             f.sort_order AS SortOrder,
                                             COUNT(c.id) AS ChatCount
                                         FROM folders f
                                         LEFT JOIN chats c ON c.folder_id = f.id AND c.user_id = f.user_id
                                         WHERE f.user_id = @UserId
                                         GROUP BY f.id, f.name, f.sort_order
                                         ORDER BY f.sort_order, f.name
                                         """;

    public async ValueTask<Outcome<GetFoldersResponse>> Handle(GetFoldersQuery request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        IEnumerable<FolderReadModel> folders = await connection.QueryAsync<FolderReadModel>
        (
            GetFoldersSql,
            new { UserId = userId }
        );

        GetFoldersResponse response = new(folders.AsList());

        return response;
    }
}