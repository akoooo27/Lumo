using System.Data.Common;

using Dapper;

using Notifications.Api.Enums;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;

namespace Notifications.Api.Queries.Notifications.Get;

internal sealed class GetNotificationsHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetNotificationsQuery, GetNotificationsResponse>
{
    private const string Sql = """
                               SELECT
                                   id AS Id,
                                   category AS Category,
                                   title AS Title,
                                   body_preview AS BodyPreview,
                                   source_type AS SourceType,
                                   source_id AS SourceId,
                                   status AS Status,
                                   created_at AS CreatedAt,
                                   read_at AS ReadAt
                               FROM notifications
                               WHERE user_id = @UserId
                                 AND status != @ArchivedStatus
                               ORDER BY created_at DESC
                               """;

    public async ValueTask<Outcome<GetNotificationsResponse>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        IEnumerable<NotificationReadModel> notifications = await connection.QueryAsync<NotificationReadModel>
        (
            Sql,
            new
            {
                UserId = userId,
                ArchivedStatus = NotificationStatus.Archived.ToString()
            }
        );

        GetNotificationsResponse response = new(notifications.AsList());

        return response;
    }
}