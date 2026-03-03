using Main.Domain.Enums;

namespace Main.Api.Endpoints.Workflows.Patch;

internal sealed record Response
(
    string WorkflowId,
    WorkflowStatus Status,
    WorkflowPauseReason PauseReason,
    DateTimeOffset? NextRunAt,
    DateTimeOffset UpdatedAt
);