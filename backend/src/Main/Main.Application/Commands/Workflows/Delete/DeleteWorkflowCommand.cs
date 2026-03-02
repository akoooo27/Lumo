using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Workflows.Delete;

public sealed record DeleteWorkflowCommand(string WorkflowId) : ICommand;
