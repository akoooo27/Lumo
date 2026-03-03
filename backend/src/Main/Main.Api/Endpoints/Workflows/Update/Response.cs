namespace Main.Api.Endpoints.Workflows.Update;

internal sealed record Response
(
    string WorkflowId,
    string Title,
    string ScheduleSummary,
    DateTimeOffset NextRunAt,
    DateTimeOffset UpdatedAt
);