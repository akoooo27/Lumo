namespace Main.Application.Abstractions.Workflows;

public sealed record WorkflowExecutionRequest
(
    string WorkflowId,
    string RunId,
    string ModelId,
    string Instruction,
    bool UseWebSearch
);