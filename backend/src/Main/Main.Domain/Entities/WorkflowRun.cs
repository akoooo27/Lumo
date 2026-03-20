using System.Diagnostics.CodeAnalysis;

using Main.Domain.Enums;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Entities;

public sealed class WorkflowRun : Entity<WorkflowRunId>
{
    public WorkflowId WorkflowId { get; private set; }

    public WorkflowRunStatus Status { get; private set; }

    public DateTimeOffset ScheduledFor { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? ResultMarkdown { get; private set; }

    public string? FailureMessage { get; private set; }

    public string? SkipReason { get; private set; }

    public string ModelIdUsed { get; private set; } = string.Empty;

    public bool UseWebSearchUsed { get; private set; }

    public string InstructionSnapshot { get; private set; } = string.Empty;

    public string TitleSnapshot { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    private WorkflowRun() { } // For EF Core

    [SetsRequiredMembers]
    private WorkflowRun
    (
        WorkflowRunId id,
        WorkflowId workflowId,
        WorkflowRunStatus status,
        DateTimeOffset scheduledFor,
        string modelIdUsed,
        bool useWebSearchUsed,
        string instructionSnapshot,
        string titleSnapshot,
        DateTimeOffset utcNow,
        string? skipReason = null,
        DateTimeOffset? completedAt = null
    )
    {
        Id = id;
        WorkflowId = workflowId;
        Status = status;
        ScheduledFor = scheduledFor;
        ModelIdUsed = modelIdUsed;
        UseWebSearchUsed = useWebSearchUsed;
        InstructionSnapshot = instructionSnapshot;
        TitleSnapshot = titleSnapshot;
        CreatedAt = utcNow;
        SkipReason = skipReason;
        CompletedAt = completedAt;
    }

    internal static Outcome<WorkflowRun> CreateQueued
    (
        WorkflowRunId id,
        WorkflowId workflowId,
        DateTimeOffset scheduledFor,
        string modelIdUsed,
        bool useWebSearchUsed,
        string instructionSnapshot,
        string titleSnapshot,
        DateTimeOffset utcNow
    )
    {
        if (id.IsEmpty)
            return WorkflowRunFaults.WorkflowRunIdRequired;

        if (workflowId.IsEmpty)
            return WorkflowRunFaults.WorkflowIdRequired;

        if (string.IsNullOrWhiteSpace(modelIdUsed))
            return WorkflowRunFaults.ModelIdRequired;

        if (string.IsNullOrWhiteSpace(instructionSnapshot))
            return WorkflowRunFaults.InstructionSnapshotRequired;

        if (string.IsNullOrWhiteSpace(titleSnapshot))
            return WorkflowRunFaults.TitleSnapshotRequired;

        WorkflowRun workflowRun = new
        (
            id: id,
            workflowId: workflowId,
            status: WorkflowRunStatus.Queued,
            scheduledFor: scheduledFor,
            modelIdUsed: modelIdUsed,
            useWebSearchUsed: useWebSearchUsed,
            instructionSnapshot: instructionSnapshot,
            titleSnapshot: titleSnapshot,
            utcNow: utcNow
        );

        return workflowRun;
    }

    internal static Outcome<WorkflowRun> CreateSkipped
    (
        WorkflowRunId id,
        WorkflowId workflowId,
        DateTimeOffset scheduledFor,
        string skipReason,
        string modelIdUsed,
        bool useWebSearchUsed,
        string instructionSnapshot,
        string titleSnapshot,
        DateTimeOffset utcNow
    )
    {
        if (id.IsEmpty)
            return WorkflowRunFaults.WorkflowRunIdRequired;

        if (workflowId.IsEmpty)
            return WorkflowRunFaults.WorkflowIdRequired;

        if (string.IsNullOrWhiteSpace(skipReason))
            return WorkflowRunFaults.SkipReasonRequired;

        if (string.IsNullOrWhiteSpace(modelIdUsed))
            return WorkflowRunFaults.ModelIdRequired;

        if (string.IsNullOrWhiteSpace(instructionSnapshot))
            return WorkflowRunFaults.InstructionSnapshotRequired;

        if (string.IsNullOrWhiteSpace(titleSnapshot))
            return WorkflowRunFaults.TitleSnapshotRequired;

        WorkflowRun workflowRun = new
        (
            id: id,
            workflowId: workflowId,
            status: WorkflowRunStatus.Skipped,
            scheduledFor: scheduledFor,
            modelIdUsed: modelIdUsed,
            useWebSearchUsed: useWebSearchUsed,
            instructionSnapshot: instructionSnapshot,
            titleSnapshot: titleSnapshot,
            utcNow: utcNow,
            skipReason: skipReason,
            completedAt: utcNow
        );

        return workflowRun;
    }

    internal Outcome MarkRunning(DateTimeOffset utcNow)
    {
        if (Status != WorkflowRunStatus.Queued)
            return WorkflowRunFaults.NotQueued;

        Status = WorkflowRunStatus.Running;
        StartedAt = utcNow;

        return Outcome.Success();
    }

    internal Outcome MarkSucceeded(string resultMarkdown, DateTimeOffset utcNow)
    {
        if (Status != WorkflowRunStatus.Running)
            return WorkflowRunFaults.NotRunning;

        if (string.IsNullOrWhiteSpace(resultMarkdown))
            return WorkflowRunFaults.ResultRequired;

        Status = WorkflowRunStatus.Succeeded;
        CompletedAt = utcNow;
        ResultMarkdown = resultMarkdown;

        return Outcome.Success();
    }

    internal Outcome MarkFailed(string failureMessage, DateTimeOffset utcNow)
    {
        if (Status != WorkflowRunStatus.Running && Status != WorkflowRunStatus.Queued)
            return WorkflowRunFaults.CannotFail;

        if (string.IsNullOrWhiteSpace(failureMessage))
            return WorkflowRunFaults.FailureMessageRequired;

        Status = WorkflowRunStatus.Failed;
        CompletedAt = utcNow;
        FailureMessage = failureMessage;

        return Outcome.Success();
    }
}