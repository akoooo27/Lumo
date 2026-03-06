using Main.Domain.Enums;

namespace Main.Api.Endpoints.WorkflowRuns.GetWorkflowRun;

internal sealed record Response
(
    string WorkflowRunId,
    string WorkflowId,
    WorkflowRunStatus Status,
    DateTimeOffset ScheduledFor,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? ResultMarkdown,
    string? FailureMessage,
    string? SkipReason,
    string ModelIdUsed,
    bool UseWebSearchUsed,
    string InstructionSnapshot,
    string TitleSnapshot,
    DateTimeOffset CreatedAt
);