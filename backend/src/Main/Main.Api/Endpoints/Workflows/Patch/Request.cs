using Main.Domain.Enums;

namespace Main.Api.Endpoints.Workflows.Patch;

internal sealed record Request
(
    string WorkflowId,
    WorkflowStatus? Status
);