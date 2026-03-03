using Main.Domain.Enums;

namespace Main.Api.Endpoints.Workflows.GetWorkflow;

internal sealed record Response
(
    string WorkflowId,
    string Title,
    string Instruction,
    string ModelId,
    bool UseWebSearch,
    WorkflowStatus Status,
    WorkflowPauseReason PauseReason,
    WorkflowRecurrenceKind RecurrenceKind,
    IReadOnlyList<DayOfWeek>? DaysOfWeek,
    string LocalTime,
    string TimeZoneId,
    string ScheduleSummary,
    DateTimeOffset NextRunAt,
    DateTimeOffset? LastRunAt,
    int ConsecutiveFailureCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);