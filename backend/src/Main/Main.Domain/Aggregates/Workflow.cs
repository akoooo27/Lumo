using System.Diagnostics.CodeAnalysis;

using Main.Domain.Constants;
using Main.Domain.Entities;
using Main.Domain.Enums;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Aggregates;

public sealed class Workflow : AggregateRoot<WorkflowId>
{
    private readonly List<WorkflowRun> _workflowRuns = [];

    public Guid UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Instruction { get; private set; } = string.Empty;

    public string NormalizedInstruction { get; private set; } = string.Empty;

    public string ModelId { get; private set; } = string.Empty;

    public bool UseWebSearch { get; private set; }

    public WorkflowDeliveryPolicy DeliveryPolicy { get; private set; }

    public WorkflowStatus Status { get; private set; }

    public WorkflowPauseReason PauseReason { get; private set; }

    public WorkflowRecurrenceKind RecurrenceKind { get; private set; }

    public int DaysOfWeekMask { get; private set; }

    public string LocalTime { get; private set; } = string.Empty;

    public string TimeZoneId { get; private set; } = string.Empty;

    public DateTimeOffset NextRunAt { get; private set; }

    public DateTimeOffset? LastRunAt { get; private set; }

    public int ConsecutiveFailureCount { get; private set; }

    public Guid? DispatchLeaseId { get; private set; }

    public DateTimeOffset? DispatchLeaseUntilUtc { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<WorkflowRun> WorkflowRuns => _workflowRuns.AsReadOnly();


    private Workflow() { } // For EF Core

    [SetsRequiredMembers]
    private Workflow
    (
        WorkflowId id,
        Guid userId,
        string title,
        string instruction,
        string normalizedInstruction,
        string modelId,
        bool useWebSearch,
        WorkflowDeliveryPolicy deliveryPolicy,
        WorkflowRecurrenceKind recurrenceKind,
        int daysOfWeekMask,
        string localTime,
        string timeZoneId,
        DateTimeOffset nextRunAt,
        DateTimeOffset utcNow
    )
    {
        Id = id;
        UserId = userId;
        Title = title;
        Instruction = instruction;
        NormalizedInstruction = normalizedInstruction;
        ModelId = modelId;
        UseWebSearch = useWebSearch;
        DeliveryPolicy = deliveryPolicy;
        Status = WorkflowStatus.Active;
        PauseReason = WorkflowPauseReason.None;
        RecurrenceKind = recurrenceKind;
        DaysOfWeekMask = daysOfWeekMask;
        LocalTime = localTime;
        TimeZoneId = timeZoneId;
        NextRunAt = nextRunAt;
        ConsecutiveFailureCount = 0;
        Version = 1;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static Outcome<Workflow> Create
    (
        WorkflowId id,
        Guid userId,
        string title,
        string instruction,
        string modelId,
        bool useWebSearch,
        WorkflowRecurrenceKind recurrenceKind,
        IReadOnlyList<DayOfWeek>? daysOfWeek,
        string localTime,
        string timeZoneId,
        DateTimeOffset nextRunAt,
        DateTimeOffset utcNow
    )
    {
        if (userId == Guid.Empty)
            return WorkflowFaults.UserIdRequired;

        Outcome titleOutcome = ValidateTitle(title);

        if (titleOutcome.IsFailure)
            return titleOutcome.Fault;

        Outcome instructionOutcome = ValidateInstruction(instruction);

        if (instructionOutcome.IsFailure)
            return instructionOutcome.Fault;

        if (string.IsNullOrWhiteSpace(modelId))
            return WorkflowFaults.ModelIdRequired;

        Outcome scheduleOutcome = ValidateSchedule
        (
            kind: recurrenceKind,
            daysOfWeek: daysOfWeek
        );

        if (scheduleOutcome.IsFailure)
            return scheduleOutcome.Fault;

        int mask = ComputeDaysOfWeekMask(recurrenceKind, daysOfWeek);
        string normalizedInstruction = NormalizeInstruction(instruction);

        Workflow workflow = new
        (
            id: id,
            userId: userId,
            title: title.Trim(),
            instruction: instruction.Trim(),
            normalizedInstruction: normalizedInstruction,
            modelId: modelId,
            useWebSearch: useWebSearch,
            deliveryPolicy: WorkflowDeliveryPolicy.InboxAndEmail,
            recurrenceKind: recurrenceKind,
            daysOfWeekMask: mask,
            localTime: localTime,
            timeZoneId: timeZoneId,
            nextRunAt: nextRunAt,
            utcNow: utcNow
        );

        return workflow;
    }

    public Outcome Update
    (
        string title,
        string instruction,
        string modelId,
        bool useWebSearch,
        WorkflowRecurrenceKind recurrenceKind,
        IReadOnlyList<DayOfWeek>? daysOfWeek,
        string localTime,
        string timeZoneId,
        DateTimeOffset nextRunAt,
        DateTimeOffset utcNow
    )
    {
        if (Status == WorkflowStatus.Archived)
            return WorkflowFaults.CannotModifyArchived;

        Outcome titleOutcome = ValidateTitle(title);

        if (titleOutcome.IsFailure)
            return titleOutcome.Fault;

        Outcome instructionOutcome = ValidateInstruction(instruction);

        if (instructionOutcome.IsFailure)
            return instructionOutcome.Fault;

        if (string.IsNullOrWhiteSpace(modelId))
            return WorkflowFaults.ModelIdRequired;

        Outcome scheduleOutcome = ValidateSchedule(recurrenceKind, daysOfWeek);

        if (scheduleOutcome.IsFailure)
            return scheduleOutcome.Fault;

        Title = title.Trim();
        Instruction = instruction.Trim();
        NormalizedInstruction = NormalizeInstruction(instruction);
        ModelId = modelId;
        UseWebSearch = useWebSearch;
        RecurrenceKind = recurrenceKind;
        DaysOfWeekMask = ComputeDaysOfWeekMask(recurrenceKind, daysOfWeek);
        LocalTime = localTime;
        TimeZoneId = timeZoneId;
        NextRunAt = nextRunAt;
        Version++;
        UpdatedAt = utcNow;

        return Outcome.Success();
    }

    public Outcome Pause(DateTimeOffset utcNow)
    {
        if (Status == WorkflowStatus.Archived)
            return WorkflowFaults.CannotModifyArchived;

        if (Status == WorkflowStatus.Paused)
            return WorkflowFaults.AlreadyPaused;

        Status = WorkflowStatus.Paused;
        PauseReason = WorkflowPauseReason.UserAction;
        Version++;
        UpdatedAt = utcNow;

        return Outcome.Success();
    }

    public Outcome Resume(DateTimeOffset nextRunAt, DateTimeOffset utcNow)
    {
        if (Status == WorkflowStatus.Archived)
            return WorkflowFaults.CannotModifyArchived;

        if (Status != WorkflowStatus.Paused)
            return WorkflowFaults.NotPaused;

        Status = WorkflowStatus.Active;
        PauseReason = WorkflowPauseReason.None;
        ConsecutiveFailureCount = 0;
        NextRunAt = nextRunAt;
        Version++;
        UpdatedAt = utcNow;

        return Outcome.Success();
    }

    public Outcome Archive(DateTimeOffset utcNow)
    {
        if (Status == WorkflowStatus.Archived)
            return WorkflowFaults.AlreadyArchived;

        Status = WorkflowStatus.Archived;
        PauseReason = WorkflowPauseReason.None;
        DispatchLeaseId = null;
        DispatchLeaseUntilUtc = null;
        Version++;
        UpdatedAt = utcNow;

        return Outcome.Success();
    }

    public bool TryClaimDispatchLease
    (
        Guid leaseId,
        DateTimeOffset leaseUntilUtc,
        DateTimeOffset utcNow
    )
    {
        if (leaseId == Guid.Empty)
            return false;

        if (leaseUntilUtc <= utcNow)
            return false;

        if (Status != WorkflowStatus.Active)
            return false;

        if (NextRunAt > utcNow)
            return false;

        if (DispatchLeaseUntilUtc.HasValue && DispatchLeaseUntilUtc.Value > utcNow)
            return false;

        DispatchLeaseId = leaseId;
        DispatchLeaseUntilUtc = leaseUntilUtc;
        Version++;

        return true;
    }

    public void ClearDispatchLease()
    {
        DispatchLeaseId = null;
        DispatchLeaseUntilUtc = null;
        Version++;
    }

    public void AdvanceNextRunAt(DateTimeOffset nextRunAt, DateTimeOffset utcNow)
    {
        NextRunAt = nextRunAt;
        Version++;
        UpdatedAt = utcNow;
    }

    public void RecordWorkflowRunSuccess(DateTimeOffset utcNow)
    {
        LastRunAt = utcNow;
        ConsecutiveFailureCount = 0;
        Version++;
        UpdatedAt = utcNow;
    }

    public bool RecordWorkflowRunFailure(DateTimeOffset utcNow)
    {
        LastRunAt = utcNow;
        ConsecutiveFailureCount++;
        Version++;
        UpdatedAt = utcNow;

        if (ConsecutiveFailureCount >= WorkflowConstants.MaxConsecutiveFailures)
        {
            Status = WorkflowStatus.Paused;
            PauseReason = WorkflowPauseReason.ConsecutiveFailures;
            return true;
        }

        return false;
    }

    public void PauseForModelUnavailable(DateTimeOffset utcNow)
    {
        Status = WorkflowStatus.Paused;
        PauseReason = WorkflowPauseReason.ModelUnavailable;
        Version++;
        UpdatedAt = utcNow;
    }

    public Outcome<WorkflowRun> CreateQueuedWorkflowRun
    (
        WorkflowRunId workflowRunId,
        DateTimeOffset scheduledFor,
        DateTimeOffset utcNow
    )
    {
        Outcome<WorkflowRun> workflowRunOutcome = WorkflowRun.CreateQueued
        (
            id: workflowRunId,
            workflowId: Id,
            scheduledFor: scheduledFor,
            modelIdUsed: ModelId,
            useWebSearchUsed: UseWebSearch,
            instructionSnapshot: Instruction,
            titleSnapshot: Title,
            utcNow: utcNow
        );

        if (workflowRunOutcome.IsFailure)
            return workflowRunOutcome.Fault;

        WorkflowRun workflowRun = workflowRunOutcome.Value;

        _workflowRuns.Add(workflowRun);

        return workflowRun;
    }

    public Outcome<WorkflowRun> CreateSkippedWorkflowRun
    (
        WorkflowRunId workflowRunId,
        DateTimeOffset scheduledFor,
        string skipReason,
        DateTimeOffset utcNow
    )
    {
        Outcome<WorkflowRun> workflowRunOutcome = WorkflowRun.CreateSkipped
        (
            id: workflowRunId,
            workflowId: Id,
            scheduledFor: scheduledFor,
            skipReason: skipReason,
            modelIdUsed: ModelId,
            useWebSearchUsed: UseWebSearch,
            instructionSnapshot: Instruction,
            titleSnapshot: Title,
            utcNow: utcNow
        );

        if (workflowRunOutcome.IsFailure)
            return workflowRunOutcome.Fault;

        WorkflowRun workflowRun = workflowRunOutcome.Value;

        _workflowRuns.Add(workflowRun);

        return workflowRun;
    }

    public Outcome StartWorkflowRun(WorkflowRunId workflowRunId, DateTimeOffset utcNow)
    {
        WorkflowRun? workflowRun = _workflowRuns.FirstOrDefault(r => r.Id == workflowRunId);

        if (workflowRun is null)
            return WorkflowRunFaults.NotFound;

        Outcome startOutcome = workflowRun.MarkRunning(utcNow);

        if (startOutcome.IsFailure)
            return startOutcome.Fault;

        return Outcome.Success();
    }

    public Outcome CompleteWorkflowRunWithSuccess(WorkflowRunId workflowRunId, string resultMarkdown, DateTimeOffset utcNow)
    {
        WorkflowRun? workflowRun = _workflowRuns.FirstOrDefault(r => r.Id == workflowRunId);

        if (workflowRun is null)
            return WorkflowRunFaults.NotFound;

        Outcome successOutcome = workflowRun.MarkSucceeded(resultMarkdown, utcNow);

        if (successOutcome.IsFailure)
            return successOutcome.Fault;

        return Outcome.Success();
    }

    public Outcome CompleteWorkflowRunWithFailure(WorkflowRunId workflowRunId, string failureMessage, DateTimeOffset utcNow)
    {
        WorkflowRun? workflowRun = _workflowRuns.FirstOrDefault(r => r.Id == workflowRunId);

        if (workflowRun is null)
            return WorkflowRunFaults.NotFound;

        Outcome failureOutcome = workflowRun.MarkFailed(failureMessage, utcNow);

        if (failureOutcome.IsFailure)
            return failureOutcome.Fault;

        return Outcome.Success();
    }

    private static Outcome ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return WorkflowFaults.TitleRequired;

        if (title.Length > WorkflowConstants.MaxTitleLength)
            return WorkflowFaults.TitleTooLong;

        return Outcome.Success();
    }

    private static Outcome ValidateInstruction(string instruction)
    {
        if (string.IsNullOrWhiteSpace(instruction))
            return WorkflowFaults.InstructionRequired;

        if (instruction.Length > WorkflowConstants.MaxInstructionLength)
            return WorkflowFaults.InstructionTooLong;

        return Outcome.Success();
    }

    private static Outcome ValidateSchedule
    (
        WorkflowRecurrenceKind kind,
        IReadOnlyList<DayOfWeek>? daysOfWeek
    )
    {
        if (!Enum.IsDefined(kind))
            return WorkflowFaults.InvalidRecurrenceKind;

        if (kind == WorkflowRecurrenceKind.Weekly && (daysOfWeek is null || daysOfWeek.Count == 0))
            return WorkflowFaults.WeeklyRequiresDays;

        if (daysOfWeek is not null && daysOfWeek.Any(d => !Enum.IsDefined(d)))
            return WorkflowFaults.InvalidDayOfWeek;

        return Outcome.Success();
    }

    internal static int ComputeDaysOfWeekMask(WorkflowRecurrenceKind kind, IReadOnlyList<DayOfWeek>? daysOfWeek)
    {
        return kind switch
        {
            WorkflowRecurrenceKind.Daily => 0b1111111, // all days
            WorkflowRecurrenceKind.Weekdays => 0b0111110, // Mon-Fri (Mon=1 .. Fri=5)
            WorkflowRecurrenceKind.Weekly => daysOfWeek?.Aggregate(0, (mask, day) => mask | (1 << (int)day)) ?? 0,
            _ => 0
        };
    }

    internal static string NormalizeInstruction(string instruction) =>
        instruction.Trim().ToUpperInvariant();
}