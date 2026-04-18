using System.Data.Common;

using Dapper;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.GoogleConnections.GetConnection;

internal sealed class GetGoogleConnectionHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetGoogleConnectionQuery, GetGoogleConnectionResponse>
{
    private const string GetConnectionSql = """
                                            SELECT google_email
                                            FROM google_connections
                                            WHERE user_id = @UserId
                                            LIMIT 1
                                            """;

    public async ValueTask<Outcome<GetGoogleConnectionResponse>> Handle(GetGoogleConnectionQuery request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        string? googleEmail = await connection.QuerySingleOrDefaultAsync<string?>
        (
            GetConnectionSql,
            new { UserId = userId }
        );

        GetGoogleConnectionResponse response = googleEmail is null
            ? new GetGoogleConnectionResponse(IsConnected: false, GoogleEmail: null)
            : new GetGoogleConnectionResponse(IsConnected: true, GoogleEmail: googleEmail);

        return response;
    }
}