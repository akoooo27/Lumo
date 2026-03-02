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

namespace Main.Application.Commands.Workflows.Update;

internal sealed class UpdateWorkflowHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IModelRegistry modelRegistry,
    IWorkflowScheduleService workflowScheduleService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateWorkflowCommand, UpdateWorkflowResponse>
{
    public async ValueTask<Outcome<UpdateWorkflowResponse>> Handle(UpdateWorkflowCommand request, CancellationToken cancellationToken)
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

        ModelInfo? modelInfo = modelRegistry.GetModelInfo(request.ModelId);

        if (modelInfo is null)
            return WorkflowOperationFaults.InvalidModel;

        Outcome scheduleInputOutcome = workflowScheduleService.ValidateScheduleInputs
        (
            localTime: request.LocalTime,
            timeZoneId: request.TimeZoneId
        );

        if (scheduleInputOutcome.IsFailure)
            return scheduleInputOutcome.Fault;

        string title = string.IsNullOrWhiteSpace(request.Title)
            ? GenerateTitle(request.Instruction)
            : request.Title;

        string scheduleSummary = workflowScheduleService.BuildScheduleSummary
        (
            kind: request.RecurrenceKind,
            daysOfWeek: request.DaysOfWeek,
            localTime: request.LocalTime,
            timeZoneId: request.TimeZoneId
        );

        DateTimeOffset utcNow = dateTimeProvider.UtcNow;

        DateTimeOffset nextRunAt = workflowScheduleService.GetNextOccurrence
        (
            kind: request.RecurrenceKind,
            daysOfWeek: request.DaysOfWeek,
            localTime: request.LocalTime,
            timeZoneId: request.TimeZoneId,
            fromUtc: utcNow
        );

        Outcome updateOutcome = workflow.Update
        (
            title: title,
            instruction: request.Instruction,
            modelId: request.ModelId,
            useWebSearch: request.UseWebSearch,
            recurrenceKind: request.RecurrenceKind,
            daysOfWeek: request.DaysOfWeek,
            localTime: request.LocalTime,
            timeZoneId: request.TimeZoneId,
            scheduleSummary: scheduleSummary,
            nextRunAt: nextRunAt,
            utcNow: utcNow
        );

        if (updateOutcome.IsFailure)
            return updateOutcome.Fault;

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

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return WorkflowOperationFaults.DuplicateWorkflow;
        }

        UpdateWorkflowResponse response = new
        (
            WorkflowId: workflow.Id.Value,
            Title: workflow.Title,
            ScheduleSummary: workflow.ScheduleSummary,
            NextRunAt: workflow.NextRunAt,
            UpdatedAt: workflow.UpdatedAt
        );

        return response;
    }

    private static string GenerateTitle(string instruction)
    {
        const int maxWords = 8;
        string trimmed = instruction.Trim();

        string[] words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        string title = words.Length <= maxWords
            ? trimmed
            : string.Join(' ', words.Take(maxWords)) + "...";

        return title.Length > WorkflowConstants.MaxTitleLength
            ? title[..WorkflowConstants.MaxTitleLength]
            : title;
    }
}
