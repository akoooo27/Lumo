namespace Main.Application.Queries.Workflows.GetWorkflows;

public sealed record GetWorkflowsResponse(IReadOnlyList<WorkflowListItemReadModel> Workflows);
