using FluentAssertions;

using Main.Domain.Constants;
using Main.Domain.Entities;
using Main.Domain.Enums;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Tests.Entities;

public sealed class WorkflowRunTests
{
    private static readonly DateTimeOffset UtcNow = DateTimeOffset.UtcNow;
    private static readonly WorkflowRunId ValidWorkflowRunId = WorkflowRunId.UnsafeFrom("wfr_01JGX123456789012345678901");
    private static readonly WorkflowId ValidWorkflowId = WorkflowId.UnsafeFrom("wfl_01JGX123456789012345678901");
    private const string ValidModelId = "gpt-5-mini";
    private const string ValidInstructionSnapshot = "Summarize the key updates";
    private const string ValidTitleSnapshot = "Daily Brief";
    private const string ValidScheduleSummarySnapshot = "Every day at 09:00";

    [Fact]
    public void CreateQueued_WithValidData_ShouldReturnSuccess()
    {
        DateTimeOffset scheduledFor = UtcNow.AddHours(1);

        Outcome<WorkflowRun> outcome = WorkflowRun.CreateQueued(
            id: ValidWorkflowRunId,
            workflowId: ValidWorkflowId,
            scheduledFor: scheduledFor,
            modelIdUsed: ValidModelId,
            useWebSearchUsed: true,
            instructionSnapshot: ValidInstructionSnapshot,
            titleSnapshot: ValidTitleSnapshot,
            scheduleSummarySnapshot: ValidScheduleSummarySnapshot,
            utcNow: UtcNow);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Id.Should().Be(ValidWorkflowRunId);
        outcome.Value.WorkflowId.Should().Be(ValidWorkflowId);
        outcome.Value.Status.Should().Be(WorkflowRunStatus.Queued);
        outcome.Value.ScheduledFor.Should().Be(scheduledFor);
        outcome.Value.ModelIdUsed.Should().Be(ValidModelId);
        outcome.Value.UseWebSearchUsed.Should().BeTrue();
        outcome.Value.InstructionSnapshot.Should().Be(ValidInstructionSnapshot);
        outcome.Value.TitleSnapshot.Should().Be(ValidTitleSnapshot);
        outcome.Value.ScheduleSummarySnapshot.Should().Be(ValidScheduleSummarySnapshot);
        outcome.Value.CreatedAt.Should().Be(UtcNow);
        outcome.Value.StartedAt.Should().BeNull();
        outcome.Value.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void CreateQueued_WithEmptyWorkflowRunId_ShouldReturnFailure()
    {
        Outcome<WorkflowRun> outcome = WorkflowRun.CreateQueued(
            id: WorkflowRunId.UnsafeFrom(string.Empty),
            workflowId: ValidWorkflowId,
            scheduledFor: UtcNow,
            modelIdUsed: ValidModelId,
            useWebSearchUsed: true,
            instructionSnapshot: ValidInstructionSnapshot,
            titleSnapshot: ValidTitleSnapshot,
            scheduleSummarySnapshot: ValidScheduleSummarySnapshot,
            utcNow: UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.WorkflowRunIdRequired);
    }

    [Fact]
    public void CreateQueued_WithEmptyWorkflowId_ShouldReturnFailure()
    {
        Outcome<WorkflowRun> outcome = WorkflowRun.CreateQueued(
            id: ValidWorkflowRunId,
            workflowId: WorkflowId.UnsafeFrom(string.Empty),
            scheduledFor: UtcNow,
            modelIdUsed: ValidModelId,
            useWebSearchUsed: true,
            instructionSnapshot: ValidInstructionSnapshot,
            titleSnapshot: ValidTitleSnapshot,
            scheduleSummarySnapshot: ValidScheduleSummarySnapshot,
            utcNow: UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.WorkflowIdRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateQueued_WithEmptyModelId_ShouldReturnFailure(string? modelId)
    {
        Outcome<WorkflowRun> outcome = WorkflowRun.CreateQueued(
            id: ValidWorkflowRunId,
            workflowId: ValidWorkflowId,
            scheduledFor: UtcNow,
            modelIdUsed: modelId!,
            useWebSearchUsed: true,
            instructionSnapshot: ValidInstructionSnapshot,
            titleSnapshot: ValidTitleSnapshot,
            scheduleSummarySnapshot: ValidScheduleSummarySnapshot,
            utcNow: UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.ModelIdRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateQueued_WithEmptyInstructionSnapshot_ShouldReturnFailure(string? instructionSnapshot)
    {
        Outcome<WorkflowRun> outcome = WorkflowRun.CreateQueued(
            id: ValidWorkflowRunId,
            workflowId: ValidWorkflowId,
            scheduledFor: UtcNow,
            modelIdUsed: ValidModelId,
            useWebSearchUsed: true,
            instructionSnapshot: instructionSnapshot!,
            titleSnapshot: ValidTitleSnapshot,
            scheduleSummarySnapshot: ValidScheduleSummarySnapshot,
            utcNow: UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.InstructionSnapshotRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateQueued_WithEmptyTitleSnapshot_ShouldReturnFailure(string? titleSnapshot)
    {
        Outcome<WorkflowRun> outcome = WorkflowRun.CreateQueued(
            id: ValidWorkflowRunId,
            workflowId: ValidWorkflowId,
            scheduledFor: UtcNow,
            modelIdUsed: ValidModelId,
            useWebSearchUsed: true,
            instructionSnapshot: ValidInstructionSnapshot,
            titleSnapshot: titleSnapshot!,
            scheduleSummarySnapshot: ValidScheduleSummarySnapshot,
            utcNow: UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.TitleSnapshotRequired);
    }

    [Fact]
    public void CreateSkipped_WithValidData_ShouldReturnSuccess()
    {
        DateTimeOffset scheduledFor = UtcNow.AddHours(1);

        Outcome<WorkflowRun> outcome = WorkflowRun.CreateSkipped(
            id: ValidWorkflowRunId,
            workflowId: ValidWorkflowId,
            scheduledFor: scheduledFor,
            skipReason: "A newer run already exists.",
            modelIdUsed: ValidModelId,
            useWebSearchUsed: false,
            instructionSnapshot: ValidInstructionSnapshot,
            titleSnapshot: ValidTitleSnapshot,
            scheduleSummarySnapshot: ValidScheduleSummarySnapshot,
            utcNow: UtcNow);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Status.Should().Be(WorkflowRunStatus.Skipped);
        outcome.Value.SkipReason.Should().Be("A newer run already exists.");
        outcome.Value.ScheduledFor.Should().Be(scheduledFor);
        outcome.Value.CompletedAt.Should().Be(UtcNow);
        outcome.Value.UseWebSearchUsed.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateSkipped_WithEmptySkipReason_ShouldReturnFailure(string? skipReason)
    {
        Outcome<WorkflowRun> outcome = WorkflowRun.CreateSkipped(
            id: ValidWorkflowRunId,
            workflowId: ValidWorkflowId,
            scheduledFor: UtcNow,
            skipReason: skipReason!,
            modelIdUsed: ValidModelId,
            useWebSearchUsed: true,
            instructionSnapshot: ValidInstructionSnapshot,
            titleSnapshot: ValidTitleSnapshot,
            scheduleSummarySnapshot: ValidScheduleSummarySnapshot,
            utcNow: UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.SkipReasonRequired);
    }

    [Fact]
    public void MarkRunning_WhenQueued_ShouldUpdateStatusAndStartedAt()
    {
        WorkflowRun workflowRun = CreateQueuedWorkflowRun();
        DateTimeOffset startedAt = UtcNow.AddMinutes(1);

        Outcome outcome = workflowRun.MarkRunning(startedAt);

        outcome.IsSuccess.Should().BeTrue();
        workflowRun.Status.Should().Be(WorkflowRunStatus.Running);
        workflowRun.StartedAt.Should().Be(startedAt);
    }

    [Fact]
    public void MarkRunning_WhenNotQueued_ShouldReturnFailure()
    {
        WorkflowRun workflowRun = CreateQueuedWorkflowRun();
        workflowRun.MarkRunning(UtcNow);

        Outcome outcome = workflowRun.MarkRunning(UtcNow.AddMinutes(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.NotQueued);
    }

    [Fact]
    public void MarkSucceeded_WhenRunning_ShouldStoreResultAndPreview()
    {
        WorkflowRun workflowRun = CreateQueuedWorkflowRun();
        workflowRun.MarkRunning(UtcNow);
        string resultMarkdown = new('a', WorkflowConstants.ResultPreviewLength + 10);
        DateTimeOffset completedAt = UtcNow.AddMinutes(1);

        Outcome outcome = workflowRun.MarkSucceeded(resultMarkdown, completedAt);

        outcome.IsSuccess.Should().BeTrue();
        workflowRun.Status.Should().Be(WorkflowRunStatus.Succeeded);
        workflowRun.CompletedAt.Should().Be(completedAt);
        workflowRun.ResultMarkdown.Should().Be(resultMarkdown);
        workflowRun.ResultPreview.Should().Be(resultMarkdown[..WorkflowConstants.ResultPreviewLength]);
    }

    [Fact]
    public void MarkSucceeded_WhenNotRunning_ShouldReturnFailure()
    {
        WorkflowRun workflowRun = CreateQueuedWorkflowRun();

        Outcome outcome = workflowRun.MarkSucceeded("Result", UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.NotRunning);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void MarkSucceeded_WithEmptyResult_ShouldReturnFailure(string? resultMarkdown)
    {
        WorkflowRun workflowRun = CreateQueuedWorkflowRun();
        workflowRun.MarkRunning(UtcNow);

        Outcome outcome = workflowRun.MarkSucceeded(resultMarkdown!, UtcNow.AddMinutes(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.ResultRequired);
    }

    [Fact]
    public void MarkFailed_WhenQueued_ShouldUpdateStatusAndFailureMessage()
    {
        WorkflowRun workflowRun = CreateQueuedWorkflowRun();
        DateTimeOffset completedAt = UtcNow.AddMinutes(1);

        Outcome outcome = workflowRun.MarkFailed("Dispatch failed.", completedAt);

        outcome.IsSuccess.Should().BeTrue();
        workflowRun.Status.Should().Be(WorkflowRunStatus.Failed);
        workflowRun.CompletedAt.Should().Be(completedAt);
        workflowRun.FailureMessage.Should().Be("Dispatch failed.");
    }

    [Fact]
    public void MarkFailed_WhenSucceeded_ShouldReturnFailure()
    {
        WorkflowRun workflowRun = CreateQueuedWorkflowRun();
        workflowRun.MarkRunning(UtcNow);
        workflowRun.MarkSucceeded("Result", UtcNow.AddMinutes(1));

        Outcome outcome = workflowRun.MarkFailed("Failure", UtcNow.AddMinutes(2));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.CannotFail);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void MarkFailed_WithEmptyFailureMessage_ShouldReturnFailure(string? failureMessage)
    {
        WorkflowRun workflowRun = CreateQueuedWorkflowRun();

        Outcome outcome = workflowRun.MarkFailed(failureMessage!, UtcNow.AddMinutes(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(WorkflowRunFaults.FailureMessageRequired);
    }

    private static WorkflowRun CreateQueuedWorkflowRun() =>
        WorkflowRun.CreateQueued(
            id: ValidWorkflowRunId,
            workflowId: ValidWorkflowId,
            scheduledFor: UtcNow.AddHours(1),
            modelIdUsed: ValidModelId,
            useWebSearchUsed: true,
            instructionSnapshot: ValidInstructionSnapshot,
            titleSnapshot: ValidTitleSnapshot,
            scheduleSummarySnapshot: ValidScheduleSummarySnapshot,
            utcNow: UtcNow).Value;
}