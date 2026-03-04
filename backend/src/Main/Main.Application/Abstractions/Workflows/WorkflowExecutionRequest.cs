namespace Main.Application.Abstractions.Workflows;

public sealed record WorkflowExecutionRequest
(
    string WorkflowId,
    string WorkflowRunId,
    string ModelId,
    string Instruction,
    bool UseWebSearch
);