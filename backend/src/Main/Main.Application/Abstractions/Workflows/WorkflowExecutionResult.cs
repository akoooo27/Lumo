namespace Main.Application.Abstractions.Workflows;

public sealed record WorkflowExecutionResult
(
    bool Success,
    string? ResultMarkdown,
    string? ResultPreview,
    string? FailureMessage
);