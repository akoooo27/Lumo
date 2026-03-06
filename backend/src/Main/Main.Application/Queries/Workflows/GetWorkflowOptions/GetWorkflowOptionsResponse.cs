using Main.Domain.Enums;

namespace Main.Application.Queries.Workflows.GetWorkflowOptions;

public sealed record GetWorkflowOptionsResponse
(
    IReadOnlyList<WorkflowModelOptionReadModel> Models,
    IReadOnlyList<WorkflowRecurrenceKind> RecurrenceKinds,
    IReadOnlyList<DayOfWeek> DaysOfWeek,
    IReadOnlyList<string> TimeZoneIds
);