using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Workflows.Resume;

public sealed record ResumeWorkflowCommand(string WorkflowId) : ICommand;
