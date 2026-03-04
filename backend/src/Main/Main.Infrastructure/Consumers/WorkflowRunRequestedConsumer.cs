using Contracts.IntegrationEvents.Workflow;

using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Workflows;
using Main.Domain.Aggregates;
using Main.Domain.Entities;
using Main.Domain.Enums;
using Main.Domain.ValueObjects;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel;
using SharedKernel.Application.Messaging;

namespace Main.Infrastructure.Consumers;

internal sealed class WorkflowRunRequestedConsumer(
    IMainDbContext dbContext,
    IWorkflowExecutionService executionService,
    IModelRegistry modelRegistry,
    IMessageBus messageBus,
    IDateTimeProvider dateTimeProvider,
    ILogger<WorkflowRunRequestedConsumer> logger) : IConsumer<WorkflowRunRequested>
{
    public async Task Consume(ConsumeContext<WorkflowRunRequested> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        WorkflowRunRequested message = context.Message;

        Outcome<WorkflowId> workflowIdOutcome = WorkflowId.From(message.WorkflowId);

        if (workflowIdOutcome.IsFailure)
        {
            logger.LogError("Invalid WorkflowId in WorkflowRunRequested: {EventId}", message.EventId);
            return;
        }

        WorkflowId workflowId = workflowIdOutcome.Value;

        Outcome<WorkflowRunId> workflowRunIdOutcome = WorkflowRunId.From(message.WorkflowRunId);

        if (workflowRunIdOutcome.IsFailure)
        {
            logger.LogError("Invalid WorkflowRunId in WorkflowRunRequested: {EventId}", message.EventId);
            return;
        }

        WorkflowRunId workflowRunId = workflowRunIdOutcome.Value;

        Workflow? workflow = await dbContext.Workflows
            .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

        if (workflow is null || workflow.Status != WorkflowStatus.Active)
            return;

        WorkflowRun? workflowRun = await dbContext.WorkflowRuns
            .FirstOrDefaultAsync(r => r.Id == workflowRunId && r.WorkflowId == workflowId, cancellationToken);

        if (workflowRun is null || workflowRun.Status != WorkflowRunStatus.Queued)
            return;

        DateTimeOffset utcNow = dateTimeProvider.UtcNow;

        ModelInfo? modelInfo = modelRegistry.GetModelInfo(workflow.ModelId);

        if (modelInfo is null || !modelInfo.ModelCapabilities.SupportsFunctionCalling)
        {
            Outcome failOutcome = workflow.CompleteWorkflowRunWithFailure
            (
                workflowRunId: workflowRunId,
                failureMessage: "Model unavailable or does not support required capabilities",
                utcNow: utcNow
            );

            if (failOutcome.IsFailure)
            {
                logger.LogError("Failed to mark run as failed: {Fault}", failOutcome.Fault.Detail);
                return;
            }

            workflow.PauseForModelUnavailable(utcNow);

            await PublishNotification
            (
                workflow: workflow,
                workflowRun: workflowRun,
                category: WorkflowNotificationCategory.WorkflowPaused,
                bodyPreview: "Workflow paused: model unavailable",
                cancellationToken: cancellationToken
            );

            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        Outcome markRunningOutcome = workflow.StartWorkflowRun
        (
            workflowRunId: workflowRunId,
            utcNow: utcNow
        );

        if (markRunningOutcome.IsFailure)
        {
            logger.LogError("Failed to mark run as running: {Fault}", markRunningOutcome.Fault.Detail);
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        WorkflowExecutionRequest request = new
        (
            WorkflowId: workflow.Id.Value,
            WorkflowRunId: workflowRun.Id.Value,
            ModelId: workflow.ModelId,
            Instruction: workflow.Instruction,
            UseWebSearch: workflow.UseWebSearch
        );

        WorkflowExecutionResult result = await executionService.ExecuteAsync(request, cancellationToken);

        DateTimeOffset completedAt = dateTimeProvider.UtcNow;

        if (result.Success)
        {
            Outcome successOutcome = workflow.CompleteWorkflowRunWithSuccess
            (
                workflowRunId: workflowRunId,
                resultMarkdown: result.ResultMarkdown!,
                utcNow: completedAt
            );

            if (successOutcome.IsFailure)
            {
                logger.LogError("Failed to mark run as succeeded: {Fault}", successOutcome.Fault.Detail);
                return;
            }

            workflow.RecordWorkflowRunSuccess(completedAt);

            await PublishNotification
            (
                workflow: workflow,
                workflowRun: workflowRun,
                category: WorkflowNotificationCategory.WorkflowSucceeded,
                bodyPreview: result.ResultMarkdown ?? "Workflow run completed successfully",
                cancellationToken: cancellationToken
            );

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            Outcome failureOutcome = workflow.CompleteWorkflowRunWithFailure
            (
                workflowRunId: workflowRunId,
                failureMessage: result.FailureMessage ?? "Unknown error.",
                utcNow: completedAt
            );

            if (failureOutcome.IsFailure)
            {
                logger.LogError("Failed to mark run as failed: {Fault}", failureOutcome.Fault.Detail);
                return;
            }

            bool paused = workflow.RecordWorkflowRunFailure(completedAt);

            await PublishNotification
            (
                workflow: workflow,
                workflowRun: workflowRun,
                category: paused
                    ? WorkflowNotificationCategory.WorkflowPaused
                    : WorkflowNotificationCategory.WorkflowFailed,
                bodyPreview: result.FailureMessage ?? "Workflow run failed",
                cancellationToken: cancellationToken
            );

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task PublishNotification
    (
        Workflow workflow,
        WorkflowRun workflowRun,
        WorkflowNotificationCategory category,
        string bodyPreview,
        CancellationToken cancellationToken
    )
    {
        string? emailAddress = await dbContext.Users
            .Where(u => u.UserId == workflow.UserId)
            .Select(u => u.EmailAddress)
            .FirstOrDefaultAsync(cancellationToken);

        WorkflowRunNotificationRequested notification = new()
        {
            EventId = Guid.NewGuid(),
            OccurredAt = dateTimeProvider.UtcNow,
            UserId = workflow.UserId,
            WorkflowId = workflow.Id.Value,
            WorkflowRunId = workflowRun.Id.Value,
            IdempotencyId = Guid.NewGuid(),
            Category = category,
            Title = workflow.Title,
            BodyPreview = bodyPreview.Length > 200 ? bodyPreview[..200] : bodyPreview,
            RecipientEmailAddress = emailAddress ?? string.Empty,
            NextRunAt = workflow.NextRunAt
        };

        await messageBus.PublishAsync(notification, cancellationToken);
    }
}