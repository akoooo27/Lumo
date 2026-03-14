using Contracts.IntegrationEvents.Workflow;

using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Generators;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.Entities;
using Main.Domain.Enums;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Workflows.Trigger;

internal sealed class TriggerWorkflowHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IIdGenerator idGenerator,
    IMessageBus messageBus,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<TriggerWorkflowCommand, TriggerWorkflowResponse>
{
    public async ValueTask<Outcome<TriggerWorkflowResponse>> Handle(TriggerWorkflowCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Outcome<WorkflowId> workflowIdOutcome = WorkflowId.From(request.WorkflowId);

        if (workflowIdOutcome.IsFailure)
            return workflowIdOutcome.Fault;

        WorkflowId workflowId = workflowIdOutcome.Value;

        Workflow? workflow = await dbContext.Workflows
            .Include(w => w.WorkflowRuns
                .Where(wr => wr.Status == WorkflowRunStatus.Queued || wr.Status == WorkflowRunStatus.Running))
            .FirstOrDefaultAsync(w => w.Id == workflowId && w.UserId == userId, cancellationToken);

        if (workflow is null)
            return WorkflowOperationFaults.NotFound;

        if (workflow.Status != WorkflowStatus.Active)
            return WorkflowOperationFaults.NotActive;

        bool hasActiveRun =
            workflow.WorkflowRuns.Any(wr => wr.Status is WorkflowRunStatus.Queued or WorkflowRunStatus.Running);

        if (hasActiveRun)
            return WorkflowOperationFaults.ActiveRunInProgress;

        DateTimeOffset utcNow = dateTimeProvider.UtcNow;

        WorkflowRunId workflowRunId = idGenerator.NewWorkflowRunId();

        Outcome<WorkflowRun> workflowRunOutcome = workflow.CreateQueuedWorkflowRun
        (
            workflowRunId: workflowRunId,
            scheduledFor: utcNow,
            utcNow: utcNow
        Outcome<WorkflowRun> workflowRunOutcome = workflow.CreateQueuedWorkflowRun
        (
            workflowRunId: workflowRunId,
            scheduledFor: utcNow,
            utcNow: utcNow
        );

        if (workflowRunOutcome.IsFailure)
            return workflowRunOutcome.Fault;

        WorkflowRun workflowRun = workflowRunOutcome.Value;
            WorkflowRunId = workflowRun.Id.Value,
            UserId = workflow.UserId,
            ModelId = workflow.ModelId,
            Instruction = workflow.Instruction,
            UseWebSearch = workflow.UseWebSearch
        };

        await messageBus.PublishAsync(workflowRunRequested, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        TriggerWorkflowResponse response = new
        (
            WorkflowRunId: workflowRun.Id.Value,
            ScheduledFor: workflowRun.ScheduledFor,
            CreatedAt: workflowRun.CreatedAt
        );

        return response;
    }
}