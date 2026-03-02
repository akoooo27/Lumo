using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Workflows.GetWorkflow;

public sealed record GetWorkflowQuery(string WorkflowId) : IQuery<GetWorkflowResponse>;
