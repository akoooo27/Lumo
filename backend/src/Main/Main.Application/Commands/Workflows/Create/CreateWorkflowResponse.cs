namespace Main.Application.Commands.Workflows.Create;

public sealed record CreateWorkflowResponse
(
    string WorkflowId,
    string Title,
    DateTimeOffset NextRunAt,
    DateTimeOffset CreatedAt
);