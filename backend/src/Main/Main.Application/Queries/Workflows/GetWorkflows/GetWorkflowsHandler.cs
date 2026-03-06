using System.Data.Common;

using Dapper;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Workflows.GetWorkflows;

internal sealed class GetWorkflowsHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetWorkflowsQuery, GetWorkflowsResponse>
{
    private const string Sql = """
        SELECT
            id AS WorkflowId,
            title AS Title,
            status AS Status,
            pause_reason AS PauseReason,
            model_id AS ModelId,
            use_web_search AS UseWebSearch,
            recurrence_kind AS RecurrenceKind,
            days_of_week_mask AS DaysOfWeekMask,
            local_time AS LocalTime,
            time_zone_id AS TimeZoneId,
            next_run_at AS NextRunAt,
            last_run_at AS LastRunAt
        FROM workflows
        WHERE user_id = @UserId
          AND status != 'Archived'
        ORDER BY created_at DESC
        """;

    public async ValueTask<Outcome<GetWorkflowsResponse>> Handle(GetWorkflowsQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        IEnumerable<WorkflowListItemReadModel> workflows =
            await connection.QueryAsync<WorkflowListItemReadModel>(Sql, new { UserId = userContext.UserId });

        return new GetWorkflowsResponse(workflows.AsList());
    }
}