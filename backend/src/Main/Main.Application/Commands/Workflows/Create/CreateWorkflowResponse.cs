namespace Main.Application.Commands.Workflows.Create;

public sealed record CreateWorkflowResponse
(
    string WorkflowId,
    string Title,
    string ScheduleSummary,
    DateTimeOffset NextRunAt,
    DateTimeOffset CreatedAt
);
