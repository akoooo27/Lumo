namespace Main.Application.Queries.Workflows.GetWorkflowRuns;

public sealed record GetWorkflowRunsResponse(IReadOnlyList<WorkflowRunListItemReadModel> WorkflowRuns);