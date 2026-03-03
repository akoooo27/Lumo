using Main.Domain.Enums;

namespace Main.Application.Queries.Workflows.GetWorkflow;

public sealed record GetWorkflowResponse
{
    public required string WorkflowId { get; init; }

    public required string Title { get; init; }

    public required string Instruction { get; init; }

    public required string ModelId { get; init; }

    public bool UseWebSearch { get; init; }

    public WorkflowStatus Status { get; init; }

    public WorkflowPauseReason PauseReason { get; init; }

    public WorkflowRecurrenceKind RecurrenceKind { get; init; }

    public required IReadOnlyList<DayOfWeek>? DaysOfWeek { get; init; }

    public required string LocalTime { get; init; }

    public required string TimeZoneId { get; init; }

    public DateTimeOffset NextRunAt { get; init; }

    public DateTimeOffset? LastRunAt { get; init; }

    public int ConsecutiveFailureCount { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}