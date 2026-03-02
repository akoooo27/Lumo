using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Workflows.GetWorkflowRun;

public sealed record GetWorkflowRunQuery(string WorkflowRunId) : IQuery<GetWorkflowRunResponse>;
