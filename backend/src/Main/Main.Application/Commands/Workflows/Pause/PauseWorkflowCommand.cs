using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Workflows.Pause;

public sealed record PauseWorkflowCommand(string WorkflowId) : ICommand;
