using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Workflows.Trigger;

public sealed record TriggerWorkflowCommand(string WorkflowId) : ICommand<TriggerWorkflowResponse>;