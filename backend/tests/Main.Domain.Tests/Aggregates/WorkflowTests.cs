using FluentAssertions;

using Main.Domain.Aggregates;
using Main.Domain.Constants;
using Main.Domain.Entities;
using Main.Domain.Enums;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Tests.Aggregates;

public sealed class WorkflowTests
{
    private static readonly DateTimeOffset UtcNow = DateTimeOffset.UtcNow;
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private static readonly WorkflowId ValidWorkflowId = WorkflowId.UnsafeFrom("wfl_01JGX123456789012345678901");
    private static readonly WorkflowRunId ValidWorkflowRunId = WorkflowRunId.UnsafeFrom("wfr_01JGX123456789012345678901");
    private const string ValidTitle = "Daily Brief";
    private const string ValidInstruction = "Summarize the key updates";
    private const string ValidModelId = "gpt-5-mini";
    private const string ValidLocalTime = "09:00";
    private const string ValidTimeZoneId = "America/New_York";
    private const string ValidScheduleSummary = "Every day at 09:00";

    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        DateTimeOffset nextRunAt = UtcNow.AddHours(1);

        Outcome<Workflow> outcome = CreateWorkflow(nextRunAt: nextRunAt);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Id.Should().Be(ValidWorkflowId);
        outcome.Value.UserId.Should().Be(ValidUserId);
        outcome.Value.Title.Should().Be(ValidTitle);
        outcome.Value.Instruction.Should().Be(ValidInstruction);
        outcome.Value.NormalizedInstruction.Should().Be(ValidInstruction.ToUpperInvariant());
        outcome.Value.ModelId.Should().Be(ValidModelId);
        outcome.Value.UseWebSearch.Should().BeTrue();
        outcome.Value.DeliveryPolicy.Should().Be(WorkflowDeliveryPolicy.InboxAndEmail);
        outcome.Value.Status.Should().Be(WorkflowStatus.Active);
        outcome.Value.PauseReason.Should().Be(WorkflowPauseReason.None);
        outcome.Value.RecurrenceKind.Should().Be(WorkflowRecurrenceKind.Daily);
        outcome.Value.DaysOfWeekMask.Should().Be(0b1111111);
        outcome.Value.LocalTime.Should().Be(ValidLocalTime);
        outcome.Value.TimeZoneId.Should().Be(ValidTimeZoneId);
        outcome.Value.ScheduleSummary.Should().Be(ValidScheduleSummary);
        outcome.Value.NextRunAt.Should().Be(nextRunAt);
        outcome.Value.LastRunAt.Should().BeNull();
        outcome.Value.ConsecutiveFailureCount.Should().Be(0);
        outcome.Value.DispatchLeaseId.Should().BeNull();
        outcome.Value.DispatchLeaseUntilUtc.Should().BeNull();
        outcome.Value.CreatedAt.Should().Be(UtcNow);
        outcome.Value.UpdatedAt.Should().Be(UtcNow);
        outcome.Value.WorkflowRuns.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithTrimmedValues_ShouldNormalizeStoredValues()
    {
        Outcome<Workflow> outcome = CreateWorkflow(
            title: "  Daily Brief  ",
            instruction: "  summarize the key updates  ");

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Title.Should().Be(ValidTitle);
        outcome.Value.Instruction.Should().Be("summarize the key updates");
        outcome.Value.NormalizedInstruction.Should().Be("SUMMARIZE THE KEY UPDATES");
    }

    [Fact]
    public void Create_WithWeeklySchedule_ShouldComputeDaysOfWeekMask()
    {
        DayOfWeek[] daysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday];

        Outcome<Workflow> outcome = CreateWorkflow(
            recurrenceKind: WorkflowRecurrenceKind.Weekly,
            daysOfWeek: daysOfWeek);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.DaysOfWeekMask.Should().Be(
            (1 << (int)DayOfWeek.Monday) |
            (1 << (int)DayOfWeek.Wednesday) |
            (1 << (int)DayOfWeek.Friday));
    }

    [Fact]
    public void Create_WithWeekdaysSchedule_ShouldComputeDaysOfWeekMask()
    {
        Outcome<Workflow> outcome = CreateWorkflow(recurrenceKind: WorkflowRecurrenceKind.Weekdays);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.DaysOfWeekMask.Should().Be(0b0111110);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldReturnFailure()
    {
        Outcome<Workflow> outcome = Workflow.Create(
            id: ValidWorkflowId,
            userId: Guid.Empty,
            title: ValidTitle,
            instruction: ValidInstruction,
            modelId: ValidModelId,
            useWebSearch: true,
            recurrenceKind: WorkflowRecurrenceKind.Daily,
            daysOfWeek: null,
            localTime: ValidLocalTime,
            timeZoneId: ValidTimeZoneId,
            scheduleSummary: ValidScheduleSummary,
            nextRunAt: UtcNow.AddHours(1),
            utcNow: UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.UserIdRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyTitle_ShouldReturnFailure(string? title)
    {
        Outcome<Workflow> outcome = CreateWorkflow(title: title!);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.TitleRequired);
    }

    [Fact]
    public void Create_WithTooLongTitle_ShouldReturnFailure()
    {
        string title = new('a', WorkflowConstants.MaxTitleLength + 1);

        Outcome<Workflow> outcome = CreateWorkflow(title: title);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.TitleTooLong);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyInstruction_ShouldReturnFailure(string? instruction)
    {
        Outcome<Workflow> outcome = CreateWorkflow(instruction: instruction!);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.InstructionRequired);
    }

    [Fact]
    public void Create_WithTooLongInstruction_ShouldReturnFailure()
    {
        string instruction = new('a', WorkflowConstants.MaxInstructionLength + 1);

        Outcome<Workflow> outcome = CreateWorkflow(instruction: instruction);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.InstructionTooLong);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyModelId_ShouldReturnFailure(string? modelId)
    {
        Outcome<Workflow> outcome = CreateWorkflow(modelId: modelId!);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.ModelIdRequired);
    }

    [Fact]
    public void Create_WithInvalidRecurrenceKind_ShouldReturnFailure()
    {
        Outcome<Workflow> outcome = CreateWorkflow(recurrenceKind: (WorkflowRecurrenceKind)999);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.InvalidRecurrenceKind);
    }

    [Fact]
    public void Create_WithWeeklyRecurrenceAndMissingDays_ShouldReturnFailure()
    {
        Outcome<Workflow> outcome = CreateWorkflow(
            recurrenceKind: WorkflowRecurrenceKind.Weekly,
            daysOfWeek: null);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.WeeklyRequiresDays);
    }

    [Fact]
    public void Create_WithWeeklyRecurrenceAndEmptyDays_ShouldReturnFailure()
    {
        Outcome<Workflow> outcome = CreateWorkflow(
            recurrenceKind: WorkflowRecurrenceKind.Weekly,
            daysOfWeek: []);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.WeeklyRequiresDays);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidData_ShouldUpdateWorkflow()
    {
        Workflow workflow = CreateValidWorkflow();
        DateTimeOffset updateTime = UtcNow.AddHours(2);
        DateTimeOffset nextRunAt = UtcNow.AddHours(5);
        DayOfWeek[] daysOfWeek = [DayOfWeek.Tuesday, DayOfWeek.Thursday];

        Outcome outcome = workflow.Update(
            title: "  Evening Brief  ",
            instruction: "  summarize market movement  ",
            modelId: "gpt-5",
            useWebSearch: false,
            recurrenceKind: WorkflowRecurrenceKind.Weekly,
            daysOfWeek: daysOfWeek,
            localTime: "18:30",
            timeZoneId: "Europe/Berlin",
            scheduleSummary: "Every Tuesday and Thursday at 18:30",
            nextRunAt: nextRunAt,
            utcNow: updateTime);

        outcome.IsSuccess.Should().BeTrue();
        workflow.Title.Should().Be("Evening Brief");
        workflow.Instruction.Should().Be("summarize market movement");
        workflow.NormalizedInstruction.Should().Be("SUMMARIZE MARKET MOVEMENT");
        workflow.ModelId.Should().Be("gpt-5");
        workflow.UseWebSearch.Should().BeFalse();
        workflow.RecurrenceKind.Should().Be(WorkflowRecurrenceKind.Weekly);
        workflow.DaysOfWeekMask.Should().Be((1 << (int)DayOfWeek.Tuesday) | (1 << (int)DayOfWeek.Thursday));
        workflow.LocalTime.Should().Be("18:30");
        workflow.TimeZoneId.Should().Be("Europe/Berlin");
        workflow.ScheduleSummary.Should().Be("Every Tuesday and Thursday at 18:30");
        workflow.NextRunAt.Should().Be(nextRunAt);
        workflow.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void Update_OnArchivedWorkflow_ShouldReturnFailure()
    {
        Workflow workflow = CreateValidWorkflow();
        workflow.Archive(UtcNow.AddMinutes(1));

        Outcome outcome = workflow.Update(
            title: "Updated title",
            instruction: ValidInstruction,
            modelId: ValidModelId,
            useWebSearch: true,
            recurrenceKind: WorkflowRecurrenceKind.Daily,
            daysOfWeek: null,
            localTime: ValidLocalTime,
            timeZoneId: ValidTimeZoneId,
            scheduleSummary: ValidScheduleSummary,
            nextRunAt: UtcNow.AddHours(2),
            utcNow: UtcNow.AddHours(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.CannotModifyArchived);
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public void Pause_WhenActive_ShouldPauseWorkflow()
    {
        Workflow workflow = CreateValidWorkflow();
        DateTimeOffset pauseTime = UtcNow.AddHours(1);

        Outcome outcome = workflow.Pause(pauseTime);

        outcome.IsSuccess.Should().BeTrue();
        workflow.Status.Should().Be(WorkflowStatus.Paused);
        workflow.PauseReason.Should().Be(WorkflowPauseReason.UserAction);
        workflow.UpdatedAt.Should().Be(pauseTime);
    }

    [Fact]
    public void Pause_WhenAlreadyPaused_ShouldReturnFailure()
    {
        Workflow workflow = CreateValidWorkflow();
        workflow.Pause(UtcNow);

        Outcome outcome = workflow.Pause(UtcNow.AddHours(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.AlreadyPaused);
    }

    [Fact]
    public void Pause_OnArchivedWorkflow_ShouldReturnFailure()
    {
        Workflow workflow = CreateValidWorkflow();
        workflow.Archive(UtcNow);

        Outcome outcome = workflow.Pause(UtcNow.AddHours(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.CannotModifyArchived);
    }

    [Fact]
    public void Resume_WhenPaused_ShouldResumeWorkflowAndResetFailureCount()
    {
        Workflow workflow = CreateValidWorkflow();
        workflow.RecordRunFailure(UtcNow.AddMinutes(1));
        workflow.RecordRunFailure(UtcNow.AddMinutes(2));
        workflow.Pause(UtcNow.AddMinutes(3));
        DateTimeOffset nextRunAt = UtcNow.AddHours(4);
        DateTimeOffset resumeTime = UtcNow.AddHours(5);

        Outcome outcome = workflow.Resume(nextRunAt, resumeTime);

        outcome.IsSuccess.Should().BeTrue();
        workflow.Status.Should().Be(WorkflowStatus.Active);
        workflow.PauseReason.Should().Be(WorkflowPauseReason.None);
        workflow.ConsecutiveFailureCount.Should().Be(0);
        workflow.NextRunAt.Should().Be(nextRunAt);
        workflow.UpdatedAt.Should().Be(resumeTime);
    }

    [Fact]
    public void Resume_WhenNotPaused_ShouldReturnFailure()
    {
        Workflow workflow = CreateValidWorkflow();

        Outcome outcome = workflow.Resume(UtcNow.AddHours(2), UtcNow.AddHours(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.NotPaused);
    }

    [Fact]
    public void Resume_OnArchivedWorkflow_ShouldReturnFailure()
    {
        Workflow workflow = CreateValidWorkflow();
        workflow.Archive(UtcNow);

        Outcome outcome = workflow.Resume(UtcNow.AddHours(2), UtcNow.AddHours(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowFaults.CannotModifyArchived);
    }

    [Fact]
    public void Archive_ShouldArchiveWorkflowAndClearDispatchLease()
    {
        Workflow workflow = CreateValidWorkflow(nextRunAt: UtcNow.AddMinutes(-5));
        Guid leaseId = Guid.NewGuid();
        workflow.TryClaimDispatchLease(leaseId, UtcNow.AddMinutes(5), UtcNow);
        DateTimeOffset archiveTime = UtcNow.AddHours(1);

        workflow.Archive(archiveTime);

        workflow.Status.Should().Be(WorkflowStatus.Archived);
        workflow.PauseReason.Should().Be(WorkflowPauseReason.None);
        workflow.DispatchLeaseId.Should().BeNull();
        workflow.DispatchLeaseUntilUtc.Should().BeNull();
        workflow.UpdatedAt.Should().Be(archiveTime);
    }

    [Fact]
    public void PauseForModelUnavailable_ShouldPauseWorkflow()
    {
        Workflow workflow = CreateValidWorkflow();
        DateTimeOffset pauseTime = UtcNow.AddHours(1);

        workflow.PauseForModelUnavailable(pauseTime);

        workflow.Status.Should().Be(WorkflowStatus.Paused);
        workflow.PauseReason.Should().Be(WorkflowPauseReason.ModelUnavailable);
        workflow.UpdatedAt.Should().Be(pauseTime);
    }

    #endregion

    #region Dispatch Tests

    [Fact]
    public void TryClaimDispatchLease_WhenEligible_ShouldSetDispatchLease()
    {
        Workflow workflow = CreateValidWorkflow(nextRunAt: UtcNow.AddMinutes(-5));
        Guid leaseId = Guid.NewGuid();
        DateTimeOffset leaseUntilUtc = UtcNow.AddMinutes(10);

        bool claimed = workflow.TryClaimDispatchLease(leaseId, leaseUntilUtc, UtcNow);

        claimed.Should().BeTrue();
        workflow.DispatchLeaseId.Should().Be(leaseId);
        workflow.DispatchLeaseUntilUtc.Should().Be(leaseUntilUtc);
    }

    [Fact]
    public void TryClaimDispatchLease_WhenWorkflowIsNotActive_ShouldReturnFalse()
    {
        Workflow workflow = CreateValidWorkflow(nextRunAt: UtcNow.AddMinutes(-5));
        workflow.Pause(UtcNow);

        bool claimed = workflow.TryClaimDispatchLease(Guid.NewGuid(), UtcNow.AddMinutes(10), UtcNow);

        claimed.Should().BeFalse();
        workflow.DispatchLeaseId.Should().BeNull();
    }

    [Fact]
    public void TryClaimDispatchLease_WhenNextRunIsInFuture_ShouldReturnFalse()
    {
        Workflow workflow = CreateValidWorkflow(nextRunAt: UtcNow.AddMinutes(5));

        bool claimed = workflow.TryClaimDispatchLease(Guid.NewGuid(), UtcNow.AddMinutes(10), UtcNow);

        claimed.Should().BeFalse();
        workflow.DispatchLeaseId.Should().BeNull();
    }

    [Fact]
    public void TryClaimDispatchLease_WhenExistingLeaseIsStillActive_ShouldReturnFalse()
    {
        Workflow workflow = CreateValidWorkflow(nextRunAt: UtcNow.AddMinutes(-5));
        workflow.TryClaimDispatchLease(Guid.NewGuid(), UtcNow.AddMinutes(5), UtcNow);

        bool claimed = workflow.TryClaimDispatchLease(Guid.NewGuid(), UtcNow.AddMinutes(10), UtcNow.AddMinutes(1));

        claimed.Should().BeFalse();
    }

    [Fact]
    public void ClearDispatchLease_ShouldClearDispatchLease()
    {
        Workflow workflow = CreateValidWorkflow(nextRunAt: UtcNow.AddMinutes(-5));
        workflow.TryClaimDispatchLease(Guid.NewGuid(), UtcNow.AddMinutes(5), UtcNow);

        workflow.ClearDispatchLease();

        workflow.DispatchLeaseId.Should().BeNull();
        workflow.DispatchLeaseUntilUtc.Should().BeNull();
    }

    [Fact]
    public void AdvanceNextRunAt_ShouldUpdateNextRunAndTimestamp()
    {
        Workflow workflow = CreateValidWorkflow();
        DateTimeOffset nextRunAt = UtcNow.AddHours(3);
        DateTimeOffset updateTime = UtcNow.AddHours(1);

        workflow.AdvanceNextRunAt(nextRunAt, updateTime);

        workflow.NextRunAt.Should().Be(nextRunAt);
        workflow.UpdatedAt.Should().Be(updateTime);
    }

    #endregion

    #region Run Recording Tests

    [Fact]
    public void RecordRunSuccess_ShouldResetFailureCountAndUpdateTimestamps()
    {
        Workflow workflow = CreateValidWorkflow();
        workflow.RecordRunFailure(UtcNow.AddMinutes(1));
        workflow.RecordRunFailure(UtcNow.AddMinutes(2));
        DateTimeOffset successTime = UtcNow.AddMinutes(3);

        workflow.RecordRunSuccess(successTime);

        workflow.LastRunAt.Should().Be(successTime);
        workflow.ConsecutiveFailureCount.Should().Be(0);
        workflow.UpdatedAt.Should().Be(successTime);
    }

    [Fact]
    public void RecordRunFailure_BeforeThreshold_ShouldIncrementFailureCount()
    {
        Workflow workflow = CreateValidWorkflow();
        DateTimeOffset failureTime = UtcNow.AddMinutes(1);

        bool reachedThreshold = workflow.RecordRunFailure(failureTime);

        reachedThreshold.Should().BeFalse();
        workflow.LastRunAt.Should().Be(failureTime);
        workflow.ConsecutiveFailureCount.Should().Be(1);
        workflow.Status.Should().Be(WorkflowStatus.Active);
        workflow.PauseReason.Should().Be(WorkflowPauseReason.None);
        workflow.UpdatedAt.Should().Be(failureTime);
    }

    [Fact]
    public void RecordRunFailure_WhenThresholdReached_ShouldPauseWorkflow()
    {
        Workflow workflow = CreateValidWorkflow();

        workflow.RecordRunFailure(UtcNow.AddMinutes(1));
        workflow.RecordRunFailure(UtcNow.AddMinutes(2));
        bool reachedThreshold = workflow.RecordRunFailure(UtcNow.AddMinutes(3));

        reachedThreshold.Should().BeTrue();
        workflow.ConsecutiveFailureCount.Should().Be(WorkflowConstants.MaxConsecutiveFailures);
        workflow.Status.Should().Be(WorkflowStatus.Paused);
        workflow.PauseReason.Should().Be(WorkflowPauseReason.ConsecutiveFailures);
        workflow.LastRunAt.Should().Be(UtcNow.AddMinutes(3));
        workflow.UpdatedAt.Should().Be(UtcNow.AddMinutes(3));
    }

    #endregion

    #region Workflow Run Tests

    [Fact]
    public void CreateQueuedRun_WithValidData_ShouldAddQueuedRun()
    {
        Workflow workflow = CreateValidWorkflow();
        DateTimeOffset scheduledFor = UtcNow.AddHours(2);
        DateTimeOffset createTime = UtcNow.AddHours(1);

        Outcome<WorkflowRun> outcome = workflow.CreateQueuedRun(ValidWorkflowRunId, scheduledFor, createTime);

        outcome.IsSuccess.Should().BeTrue();
        workflow.WorkflowRuns.Should().ContainSingle();
        outcome.Value.WorkflowId.Should().Be(workflow.Id);
        outcome.Value.Status.Should().Be(WorkflowRunStatus.Queued);
        outcome.Value.ScheduledFor.Should().Be(scheduledFor);
        outcome.Value.ModelIdUsed.Should().Be(ValidModelId);
        outcome.Value.UseWebSearchUsed.Should().BeTrue();
        outcome.Value.InstructionSnapshot.Should().Be(ValidInstruction);
        outcome.Value.TitleSnapshot.Should().Be(ValidTitle);
        outcome.Value.ScheduleSummarySnapshot.Should().Be(ValidScheduleSummary);
        outcome.Value.CreatedAt.Should().Be(createTime);
        outcome.Value.StartedAt.Should().BeNull();
        outcome.Value.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void CreateQueuedRun_WithInvalidRunId_ShouldReturnFailure()
    {
        Workflow workflow = CreateValidWorkflow();

        Outcome<WorkflowRun> outcome = workflow.CreateQueuedRun(
            WorkflowRunId.UnsafeFrom(string.Empty),
            UtcNow.AddHours(1),
            UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.WorkflowRunIdRequired);
        workflow.WorkflowRuns.Should().BeEmpty();
    }

    [Fact]
    public void CreateSkippedRun_WithValidData_ShouldAddSkippedRun()
    {
        Workflow workflow = CreateValidWorkflow();
        DateTimeOffset scheduledFor = UtcNow.AddHours(2);
        DateTimeOffset createTime = UtcNow.AddHours(1);

        Outcome<WorkflowRun> outcome = workflow.CreateSkippedRun(
            ValidWorkflowRunId,
            scheduledFor,
            "A newer run already exists.",
            createTime);

        outcome.IsSuccess.Should().BeTrue();
        workflow.WorkflowRuns.Should().ContainSingle();
        outcome.Value.Status.Should().Be(WorkflowRunStatus.Skipped);
        outcome.Value.SkipReason.Should().Be("A newer run already exists.");
        outcome.Value.CompletedAt.Should().Be(createTime);
        outcome.Value.ModelIdUsed.Should().Be(ValidModelId);
        outcome.Value.UseWebSearchUsed.Should().BeTrue();
        outcome.Value.InstructionSnapshot.Should().Be(ValidInstruction);
        outcome.Value.TitleSnapshot.Should().Be(ValidTitle);
        outcome.Value.ScheduleSummarySnapshot.Should().Be(ValidScheduleSummary);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateSkippedRun_WithEmptySkipReason_ShouldReturnFailure(string? skipReason)
    {
        Workflow workflow = CreateValidWorkflow();

        Outcome<WorkflowRun> outcome = workflow.CreateSkippedRun(
            ValidWorkflowRunId,
            UtcNow.AddHours(1),
            skipReason!,
            UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.SkipReasonRequired);
        workflow.WorkflowRuns.Should().BeEmpty();
    }

    #endregion

    private static Outcome<Workflow> CreateWorkflow(
        string title = ValidTitle,
        string instruction = ValidInstruction,
        string modelId = ValidModelId,
        WorkflowRecurrenceKind recurrenceKind = WorkflowRecurrenceKind.Daily,
        IReadOnlyList<DayOfWeek>? daysOfWeek = null,
        DateTimeOffset? nextRunAt = null)
    {
        return Workflow.Create(
            id: ValidWorkflowId,
            userId: ValidUserId,
            title: title,
            instruction: instruction,
            modelId: modelId,
            useWebSearch: true,
            recurrenceKind: recurrenceKind,
            daysOfWeek: daysOfWeek,
            localTime: ValidLocalTime,
            timeZoneId: ValidTimeZoneId,
            scheduleSummary: ValidScheduleSummary,
            nextRunAt: nextRunAt ?? UtcNow.AddHours(1),
            utcNow: UtcNow);
    }

    private static Workflow CreateValidWorkflow(
        WorkflowRecurrenceKind recurrenceKind = WorkflowRecurrenceKind.Daily,
        IReadOnlyList<DayOfWeek>? daysOfWeek = null,
        DateTimeOffset? nextRunAt = null) =>
        CreateWorkflow(
            recurrenceKind: recurrenceKind,
            daysOfWeek: daysOfWeek,
            nextRunAt: nextRunAt).Value;
}