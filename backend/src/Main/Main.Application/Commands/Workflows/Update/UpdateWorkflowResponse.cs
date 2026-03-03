namespace Main.Application.Commands.Workflows.Update;

public sealed record UpdateWorkflowResponse
(
    string WorkflowId,
    string Title,
    DateTimeOffset NextRunAt,
    DateTimeOffset UpdatedAt
);