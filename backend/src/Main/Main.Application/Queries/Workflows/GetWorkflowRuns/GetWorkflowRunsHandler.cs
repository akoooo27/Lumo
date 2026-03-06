using System.Data.Common;

using Dapper;

using Main.Application.Faults;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Workflows.GetWorkflowRuns;

internal sealed class GetWorkflowRunsHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetWorkflowRunsQuery, GetWorkflowRunsResponse>
{
    private const string GetOwnerSql = """
        SELECT user_id
        FROM workflows
        WHERE id = @WorkflowId
        """;

    private const string GetRunsSql = """
        SELECT
            id AS WorkflowRunId,
            status AS Status,
            scheduled_for AS ScheduledFor,
            started_at AS StartedAt,
            completed_at AS CompletedAt,
            failure_message AS FailureMessage,
            skip_reason AS SkipReason,
            created_at AS CreatedAt
        FROM workflow_runs
        WHERE workflow_id = @WorkflowId
        ORDER BY scheduled_for DESC, created_at DESC
        """;

    public async ValueTask<Outcome<GetWorkflowRunsResponse>> Handle(GetWorkflowRunsQuery request, CancellationToken cancellationToken)
    {
        Outcome<WorkflowId> workflowIdOutcome = WorkflowId.From(request.WorkflowId);

        if (workflowIdOutcome.IsFailure)
            return workflowIdOutcome.Fault;

        string workflowId = workflowIdOutcome.Value.Value;

        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        Guid? ownerId = await connection.QuerySingleOrDefaultAsync<Guid?>(GetOwnerSql, new { WorkflowId = workflowId });

        if (ownerId is null)
            return WorkflowFaults.NotFound;

        if (ownerId != userContext.UserId)
            return WorkflowOperationFaults.NotOwner;

        IEnumerable<WorkflowRunListItemReadModel> workflowRuns =
            await connection.QueryAsync<WorkflowRunListItemReadModel>(GetRunsSql, new { WorkflowId = workflowId });

        return new GetWorkflowRunsResponse(workflowRuns.AsList());
    }
}