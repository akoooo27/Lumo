using System.Data.Common;

using Dapper;

using Main.Application.Faults;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Memories.GetMemoryById;

internal sealed class GetMemoryByIdHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<GetMemoryByIdQuery, GetMemoryByIdResponse>
{
    private const string Sql = """
                               SELECT
                                   id               AS Id,
                                   content          AS Content,
                                   category         AS Category,
                                   created_at       AS CreatedAt,
                                   updated_at       AS UpdatedAt,
                                   last_accessed_at AS LastAccessedAt,
                                   access_count     AS AccessCount,
                                   importance       AS Importance
                               FROM memories
                               WHERE id = @MemoryId
                                 AND user_id = @UserId
                                 AND is_active = true
                               """;

    public async ValueTask<Outcome<GetMemoryByIdResponse>> Handle(GetMemoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        GetMemoryByIdResponse? result = await connection.QuerySingleOrDefaultAsync<GetMemoryByIdResponse>
        (
            Sql,
            new
            {
                request.MemoryId,
                UserId = userContext.UserId
            }
        );

        return result is null
            ? MemoryOperationFaults.NotFound
            : result;
    }
}