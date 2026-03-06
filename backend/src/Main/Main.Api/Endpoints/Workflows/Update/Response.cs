namespace Main.Api.Endpoints.Workflows.Update;

internal sealed record Response
(
    string WorkflowId,
    string Title,
    DateTimeOffset NextRunAt,
    DateTimeOffset UpdatedAt
);