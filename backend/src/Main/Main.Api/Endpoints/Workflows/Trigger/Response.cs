namespace Main.Api.Endpoints.Workflows.Trigger;

internal sealed record Response
(
    string WorkflowRunId,
    DateTimeOffset ScheduledFor,
    DateTimeOffset CreatedAt
);