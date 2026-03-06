namespace Main.Api.Endpoints.Workflows.Create;

internal sealed record Response
(
    string WorkflowId,
    string Title,
    DateTimeOffset NextRunAt,
    DateTimeOffset CreatedAt
);