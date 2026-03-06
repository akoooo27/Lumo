using System.Data.Common;

using Microsoft.Extensions.Options;

using Notifications.Api.Options;

using Npgsql;

using SharedKernel.Application.Data;

namespace Notifications.Api.Data;

internal sealed class DbConnectionFactory(IOptions<DatabaseOptions> databaseOptions) : IDbConnectionFactory
{
    private readonly DatabaseOptions _databaseOptions = databaseOptions.Value;

    public async Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        NpgsqlConnection connection = new(_databaseOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}