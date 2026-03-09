using System.Data.Common;

using Dapper;

using Main.Domain.Enums;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Workflows.GetWorkflow;

internal sealed class GetWorkflowHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetWorkflowQuery, GetWorkflowResponse>
{
    private const string Sql = """
        SELECT
            user_id AS UserId,
            id AS WorkflowId,
            title AS Title,
            instruction AS Instruction,
            model_id AS ModelId,
            use_web_search AS UseWebSearch,
            status AS Status,
            pause_reason AS PauseReason,
            recurrence_kind AS RecurrenceKind,
            days_of_week_mask AS DaysOfWeekMask,
            local_time AS LocalTime,
            time_zone_id AS TimeZoneId,
            next_run_at AS NextRunAt,
            last_run_at AS LastRunAt,
            consecutive_failure_count AS ConsecutiveFailureCount,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM workflows
        WHERE id = @WorkflowId
          AND user_id = @UserId
        """;

    public async ValueTask<Outcome<GetWorkflowResponse>> Handle(GetWorkflowQuery request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Outcome<WorkflowId> workflowIdOutcome = WorkflowId.From(request.WorkflowId);

        if (workflowIdOutcome.IsFailure)
            return workflowIdOutcome.Fault;

        WorkflowId workflowId = workflowIdOutcome.Value;

        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        WorkflowDetailsDbRow? workflow = await connection.QuerySingleOrDefaultAsync<WorkflowDetailsDbRow>
        (
            Sql,
            new
            {
                WorkflowId = workflowId.Value,
                UserId = userId
            }
        );

        if (workflow is null)
            return WorkflowFaults.NotFound;

        GetWorkflowResponse response = new()
        {
            WorkflowId = workflow.WorkflowId,
            Title = workflow.Title,
            Instruction = workflow.Instruction,
            ModelId = workflow.ModelId,
            UseWebSearch = workflow.UseWebSearch,
            Status = workflow.Status,
            PauseReason = workflow.PauseReason,
            RecurrenceKind = workflow.RecurrenceKind,
            DaysOfWeek = ExtractDaysOfWeek(workflow.RecurrenceKind, workflow.DaysOfWeekMask),
            LocalTime = workflow.LocalTime,
            TimeZoneId = workflow.TimeZoneId,
            NextRunAt = workflow.NextRunAt,
            LastRunAt = workflow.LastRunAt,
            ConsecutiveFailureCount = workflow.ConsecutiveFailureCount,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt
        };

        return response;
    }

    private static List<DayOfWeek>? ExtractDaysOfWeek(WorkflowRecurrenceKind recurrenceKind, int daysOfWeekMask)
    {
        if (recurrenceKind != WorkflowRecurrenceKind.Weekly)
            return null;

        List<DayOfWeek> daysOfWeek = [];

        foreach (DayOfWeek dayOfWeek in Enum.GetValues<DayOfWeek>())
        {
            if ((daysOfWeekMask & (1 << (int)dayOfWeek)) != 0)
                daysOfWeek.Add(dayOfWeek);
        }

        return daysOfWeek;
    }
}