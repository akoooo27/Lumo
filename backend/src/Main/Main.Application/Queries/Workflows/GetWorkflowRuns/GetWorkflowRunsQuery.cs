using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Workflows.GetWorkflowRuns;

public sealed record GetWorkflowRunsQuery(string WorkflowId) : IQuery<GetWorkflowRunsResponse>;
