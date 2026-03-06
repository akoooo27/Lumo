using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Generators;
using Main.Application.Abstractions.Workflows;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.Constants;
using Main.Domain.Enums;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Workflows.Create;

internal sealed class CreateWorkflowHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IIdGenerator idGenerator,
    IModelRegistry modelRegistry,
    IWorkflowScheduleService workflowScheduleService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateWorkflowCommand, CreateWorkflowResponse>
{
    public async ValueTask<Outcome<CreateWorkflowResponse>> Handle(CreateWorkflowCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        ModelInfo? modelInfo = modelRegistry.GetModelInfo(request.ModelId);

        if (modelInfo is null)
            return WorkflowOperationFaults.InvalidModel;

        int activeCount = await dbContext.Workflows
            .CountAsync(w => w.UserId == userId && w.Status == WorkflowStatus.Active, cancellationToken);

        if (activeCount >= WorkflowConstants.MaxActiveWorkflowsPerUser)
            return WorkflowOperationFaults.MaxWorkflowsReached;

        Outcome scheduleInputOutcome = workflowScheduleService.ValidateScheduleInputs
        (
            localTime: request.LocalTime,
            timeZoneId: request.TimeZoneId
        );

        if (scheduleInputOutcome.IsFailure)
            return scheduleInputOutcome.Fault;

        if (request.DayOfWeeks is not null && request.DayOfWeeks.Any(d => !Enum.IsDefined(d)))
            return WorkflowOperationFaults.InvalidDayOfWeek;

        string title = string.IsNullOrWhiteSpace(request.Title)
            ? WorkflowTitleGenerator.Generate(request.Instruction)
            : request.Title;

        DateTimeOffset nextRunAt = workflowScheduleService.GetNextOccurrence
        (
            kind: request.RecurrenceKind,
            daysOfWeek: request.DayOfWeeks,
            localTime: request.LocalTime,
            timeZoneId: request.TimeZoneId,
            fromUtc: dateTimeProvider.UtcNow
        );

        WorkflowId workflowId = idGenerator.NewWorkflowId();

        Outcome<Workflow> workflowOutcome = Workflow.Create
        (
            id: workflowId,
            userId: userId,
            title: title,
            instruction: request.Instruction,
            modelId: request.ModelId,
            useWebSearch: request.UseWebSearch,
            recurrenceKind: request.RecurrenceKind,
            daysOfWeek: request.DayOfWeeks,
            localTime: request.LocalTime,
            timeZoneId: request.TimeZoneId,
            nextRunAt: nextRunAt,
            utcNow: dateTimeProvider.UtcNow
        );

        if (workflowOutcome.IsFailure)
            return workflowOutcome.Fault;

        Workflow workflow = workflowOutcome.Value;

        bool isDuplicate = await dbContext.Workflows.AnyAsync(w =>
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

        await dbContext.Workflows.AddAsync(workflow, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        CreateWorkflowResponse response = new
        (
            WorkflowId: workflow.Id.Value,
            Title: workflow.Title,
            NextRunAt: workflow.NextRunAt,
            CreatedAt: workflow.CreatedAt
        );

        return response;
    }
}