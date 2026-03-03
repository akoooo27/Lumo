using Main.Domain.Enums;

namespace Main.Application.Queries.Workflows.GetWorkflowRun;

internal sealed record class WorkflowRunDetailsDbRow
{
    public Guid UserId { get; init; }

    public required string WorkflowRunId { get; init; }

    public required string WorkflowId { get; init; }

    public WorkflowRunStatus Status { get; init; }

    public DateTimeOffset ScheduledFor { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public string? ResultMarkdown { get; init; }

    public string? ResultPreview { get; init; }

    public string? FailureMessage { get; init; }

    public string? SkipReason { get; init; }

    public required string ModelIdUsed { get; init; }

    public bool UseWebSearchUsed { get; init; }

    public required string InstructionSnapshot { get; init; }

    public required string TitleSnapshot { get; init; }

    public required string ScheduleSummarySnapshot { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}