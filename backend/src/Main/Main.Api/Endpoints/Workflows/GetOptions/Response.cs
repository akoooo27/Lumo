using Main.Domain.Enums;

namespace Main.Api.Endpoints.Workflows.GetOptions;

internal sealed record WorkflowModelOptionDto
(
    string Id,
    string DisplayName,
    string Provider,
    bool IsDefault,
    bool SupportsFunctionCalling
);

internal sealed record Response
(
    IReadOnlyList<WorkflowModelOptionDto> Models,
    IReadOnlyList<WorkflowRecurrenceKind> RecurrenceKinds,
    IReadOnlyList<DayOfWeek> DaysOfWeek,
    IReadOnlyList<string> TimeZoneIds
);