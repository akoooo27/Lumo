using Main.Domain.Enums;

namespace Main.Api.Endpoints.Workflows.GetWorkflowRuns;

internal sealed record WorkflowRunListItemDto
(
    string WorkflowRunId,
    WorkflowRunStatus Status,
    DateTimeOffset ScheduledFor,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? FailureMessage,
    string? SkipReason,
    DateTimeOffset CreatedAt
);

internal sealed record Response(IReadOnlyList<WorkflowRunListItemDto> WorkflowRuns);