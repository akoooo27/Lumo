using Main.Domain.Enums;

using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Workflows.Patch;

public sealed record PatchWorkflowCommand
(
    string WorkflowId,
    WorkflowStatus? Status
) : ICommand<PatchWorkflowResponse>;