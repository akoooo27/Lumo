using Main.Domain.Enums;

namespace Main.Api.Endpoints.Workflows.GetWorkflows;

internal sealed record WorkflowListItemDto
(
    string WorkflowId,
    string Title,
    WorkflowStatus Status,
    WorkflowPauseReason PauseReason,
    string ModelId,
    bool UseWebSearch,
    string ScheduleSummary,
    DateTimeOffset NextRunAt,
    DateTimeOffset? LastRunAt
);

internal sealed record Response(IReadOnlyList<WorkflowListItemDto> Workflows);