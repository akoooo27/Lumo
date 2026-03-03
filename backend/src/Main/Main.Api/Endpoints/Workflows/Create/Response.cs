namespace Main.Api.Endpoints.Workflows.Create;

internal sealed record Response
(
    string WorkflowId,
    string Title,
    string ScheduleSummary,
    DateTimeOffset NextRunAt,
    DateTimeOffset CreatedAt
);