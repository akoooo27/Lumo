using Main.Domain.Enums;

namespace Main.Application.Queries.Workflows.GetWorkflowRuns;

public sealed record class WorkflowRunListItemReadModel
{
    public required string WorkflowRunId { get; init; }

    public WorkflowRunStatus Status { get; init; }

    public DateTimeOffset ScheduledFor { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public string? ResultPreview { get; init; }

    public string? FailureMessage { get; init; }

    public string? SkipReason { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}