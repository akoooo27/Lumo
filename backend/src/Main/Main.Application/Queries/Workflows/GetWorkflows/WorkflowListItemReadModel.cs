using Main.Domain.Enums;

namespace Main.Application.Queries.Workflows.GetWorkflows;

public sealed record class WorkflowListItemReadModel
{
    public required string WorkflowId { get; init; }

    public required string Title { get; init; }

    public WorkflowStatus Status { get; init; }

    public WorkflowPauseReason PauseReason { get; init; }

    public required string ModelId { get; init; }

    public bool UseWebSearch { get; init; }

    public required string ScheduleSummary { get; init; }

    public DateTimeOffset NextRunAt { get; init; }

    public DateTimeOffset? LastRunAt { get; init; }
}