using Main.Domain.Enums;

namespace Main.Application.Commands.Workflows.Patch;

public sealed record PatchWorkflowResponse
(
    string WorkflowId,
    WorkflowStatus Status,
    WorkflowPauseReason PauseReason,
    DateTimeOffset? NextRunAt,
    DateTimeOffset UpdatedAt
);