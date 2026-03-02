using System.Data.Common;

using Dapper;

using Main.Application.Faults;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Workflows.GetWorkflowRun;

internal sealed class GetWorkflowRunHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetWorkflowRunQuery, GetWorkflowRunResponse>
{
    private const string Sql = """
        SELECT
            w.user_id AS UserId,
            wr.id AS WorkflowRunId,
            wr.workflow_id AS WorkflowId,
            wr.status AS Status,
            wr.scheduled_for AS ScheduledFor,
            wr.started_at AS StartedAt,
            wr.completed_at AS CompletedAt,
            wr.result_markdown AS ResultMarkdown,
            wr.result_preview AS ResultPreview,
            wr.failure_message AS FailureMessage,
            wr.skip_reason AS SkipReason,
            wr.model_id_used AS ModelIdUsed,
            wr.use_web_search_used AS UseWebSearchUsed,
            wr.instruction_snapshot AS InstructionSnapshot,
            wr.title_snapshot AS TitleSnapshot,
            wr.schedule_summary_snapshot AS ScheduleSummarySnapshot,
            wr.created_at AS CreatedAt
        FROM workflow_runs wr
        INNER JOIN workflows w ON w.id = wr.workflow_id
        WHERE wr.id = @WorkflowRunId
        """;

    public async ValueTask<Outcome<GetWorkflowRunResponse>> Handle(GetWorkflowRunQuery request, CancellationToken cancellationToken)
    {
        Outcome<WorkflowRunId> workflowRunIdOutcome = WorkflowRunId.From(request.WorkflowRunId);

        if (workflowRunIdOutcome.IsFailure)
            return workflowRunIdOutcome.Fault;

        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        WorkflowRunDetailsDbRow? workflowRun = await connection.QuerySingleOrDefaultAsync<WorkflowRunDetailsDbRow>
        (
            Sql,
            new
            {
                WorkflowRunId = workflowRunIdOutcome.Value.Value
            }
        );

        if (workflowRun is null)
            return WorkflowRunFaults.NotFound;

        if (workflowRun.UserId != userContext.UserId)
            return WorkflowOperationFaults.NotOwner;

        GetWorkflowRunResponse response = new()
        {
            WorkflowRunId = workflowRun.WorkflowRunId,
            WorkflowId = workflowRun.WorkflowId,
            Status = workflowRun.Status,
            ScheduledFor = workflowRun.ScheduledFor,
            StartedAt = workflowRun.StartedAt,
            CompletedAt = workflowRun.CompletedAt,
            ResultMarkdown = workflowRun.ResultMarkdown,
            ResultPreview = workflowRun.ResultPreview,
            FailureMessage = workflowRun.FailureMessage,
            SkipReason = workflowRun.SkipReason,
            ModelIdUsed = workflowRun.ModelIdUsed,
            UseWebSearchUsed = workflowRun.UseWebSearchUsed,
            InstructionSnapshot = workflowRun.InstructionSnapshot,
            TitleSnapshot = workflowRun.TitleSnapshot,
            ScheduleSummarySnapshot = workflowRun.ScheduleSummarySnapshot,
            CreatedAt = workflowRun.CreatedAt
        };

        return response;
    }
}
