namespace Main.Application.Commands.Workflows.Update;

public sealed record UpdateWorkflowResponse
(
    string WorkflowId,
    string Title,
    string ScheduleSummary,
    DateTimeOffset NextRunAt,
    DateTimeOffset UpdatedAt
);