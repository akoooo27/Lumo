using Main.Domain.Enums;

namespace Main.Api.Endpoints.Workflows.Create;

internal sealed record Request
(
    string? Title,
    string Instruction,
    string ModelId,
    bool UseWebSearch,
    ScheduleRequest Schedule
);

internal sealed record ScheduleRequest
(
    WorkflowRecurrenceKind Kind,
    IReadOnlyList<DayOfWeek>? DaysOfWeek,
    string LocalTime,
    string TimeZoneId
);