namespace Main.Application.Abstractions.Workflows;

public sealed record WorkflowExecutionResult
(
    bool Success,
    string? ResultMarkdown,
    string? FailureMessage,
    int InputTokens,
    int OutputTokens,
    int TotalTokens
);