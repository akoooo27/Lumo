namespace Main.Application.Commands.Workflows.Trigger;

public sealed record TriggerWorkflowResponse
(
    string WorkflowRunId,
    DateTimeOffset ScheduledFor,
    DateTimeOffset CreatedAt
);