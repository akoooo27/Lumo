using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Workflows;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.Constants;
using Main.Domain.Enums;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Workflows.Patch;

internal sealed class PatchWorkflowHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IModelRegistry modelRegistry,
    IWorkflowScheduleService workflowScheduleService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<PatchWorkflowCommand, PatchWorkflowResponse>
{
    public async ValueTask<Outcome<PatchWorkflowResponse>> Handle(PatchWorkflowCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Outcome<WorkflowId> workflowIdOutcome = WorkflowId.From(request.WorkflowId);

        if (workflowIdOutcome.IsFailure)
            return workflowIdOutcome.Fault;

        WorkflowId workflowId = workflowIdOutcome.Value;

        Workflow? workflow = await dbContext.Workflows
            .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

        if (workflow is null)
            return WorkflowFaults.NotFound;

        if (workflow.UserId != userId)
            return WorkflowOperationFaults.NotOwner;

        DateTimeOffset utcNow = dateTimeProvider.UtcNow;

        if (request.Status is WorkflowStatus.Paused)
        {
            Outcome pauseOutcome = workflow.Pause(utcNow);

            if (pauseOutcome.IsFailure)
                return pauseOutcome.Fault;
        }
        else if (request.Status is WorkflowStatus.Active)
        {
            ModelInfo? modelInfo = modelRegistry.GetModelInfo(workflow.ModelId);

            if (modelInfo is null)
                return WorkflowOperationFaults.ModelNoLongerAvailable;

            Outcome scheduleInputOutcome = workflowScheduleService.ValidateScheduleInputs
            (
                localTime: workflow.LocalTime,
                timeZoneId: workflow.TimeZoneId
            );

            if (scheduleInputOutcome.IsFailure)
                return scheduleInputOutcome.Fault;

            int activeCount = await dbContext.Workflows
                .CountAsync(w => w.UserId == userId && w.Status == WorkflowStatus.Active && w.Id != workflow.Id, cancellationToken);

            if (activeCount >= WorkflowConstants.MaxActiveWorkflowsPerUser)
                return WorkflowOperationFaults.MaxWorkflowsReached;

            bool isDuplicate = await dbContext.Workflows.AnyAsync(w =>
                    w.Id != workflow.Id &&
                    w.UserId == userId &&
                    w.Status == WorkflowStatus.Active &&
                    w.NormalizedInstruction == workflow.NormalizedInstruction &&
                    w.RecurrenceKind == workflow.RecurrenceKind &&
                    w.DaysOfWeekMask == workflow.DaysOfWeekMask &&
                    w.LocalTime == workflow.LocalTime &&
                    w.TimeZoneId == workflow.TimeZoneId,
                cancellationToken
            );

            if (isDuplicate)
                return WorkflowOperationFaults.DuplicateWorkflow;

            DateTimeOffset nextRunAt = workflowScheduleService.GetNextOccurrence
            (
                kind: workflow.RecurrenceKind,
                daysOfWeek: ExtractDaysOfWeek(workflow.RecurrenceKind, workflow.DaysOfWeekMask),
                localTime: workflow.LocalTime,
                timeZoneId: workflow.TimeZoneId,
                fromUtc: utcNow
            );

            Outcome resumeOutcome = workflow.Resume(nextRunAt, utcNow);

            if (resumeOutcome.IsFailure)
                return resumeOutcome.Fault;
        }
        else if (request.Status is WorkflowStatus.Archived)
        {
            workflow.Archive(utcNow);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return WorkflowOperationFaults.DuplicateWorkflow;
        }

        return new PatchWorkflowResponse
        (
            WorkflowId: workflow.Id.Value,
            Status: workflow.Status,
            PauseReason: workflow.PauseReason,
            NextRunAt: workflow.Status == WorkflowStatus.Archived ? null : workflow.NextRunAt,
            UpdatedAt: workflow.UpdatedAt
        );
    }

    private static List<DayOfWeek>? ExtractDaysOfWeek(WorkflowRecurrenceKind recurrenceKind, int daysOfWeekMask)
    {
        if (recurrenceKind != WorkflowRecurrenceKind.Weekly)
            return null;

        List<DayOfWeek> daysOfWeek = [];

        foreach (DayOfWeek dayOfWeek in Enum.GetValues<DayOfWeek>())
        {
            if ((daysOfWeekMask & (1 << (int)dayOfWeek)) != 0)
                daysOfWeek.Add(dayOfWeek);
        }

        return daysOfWeek;
    }
}