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

        Outcome<WorkflowRunId> workflowRunIdOutcome = WorkflowRunId.From(message.RunId);

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
            Outcome failOutcome = workflow.CompleteRunWithFailure
            (
                runId: workflowRunId,
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
                category: "WorkflowFailed",
                bodyPreview: "Workflow paused: model unavailable",
                userId: workflow.UserId,
                cancellationToken: cancellationToken
            );

            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        Outcome markRunningOutcome = workflow.StartRun
        (
            runId: workflowRunId,
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
            RunId: workflowRun.Id.Value,
            ModelId: workflow.ModelId,
            Instruction: workflow.Instruction,
            UseWebSearch: workflow.UseWebSearch
        );

        WorkflowExecutionResult result = await executionService.ExecuteAsync(request, cancellationToken);

        if (result.Success)
        {
            Outcome successOutcome = workflow.CompleteRunWithSuccess
            (
                runId: workflowRunId,
                resultMarkdown: result.ResultMarkdown!,
                utcNow: dateTimeProvider.UtcNow
            );

            if (successOutcome.IsFailure)
            {
                logger.LogError("Failed to mark run as succeeded: {Fault}", successOutcome.Fault.Detail);
                return;
            }

            workflow.RecordRunSuccess(utcNow);

            await PublishNotification
            (
                workflow: workflow,
                workflowRun: workflowRun,
                category: "WorkflowSucceeded",
                bodyPreview: result.ResultMarkdown ?? "Workflow run completed successfully",
                userId: workflow.UserId,
                cancellationToken: cancellationToken
            );
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            Outcome failureOutcome = workflow.CompleteRunWithFailure
            (
                runId: workflowRunId,
                failureMessage: result.FailureMessage ?? "Unknown error.",
                utcNow: dateTimeProvider.UtcNow
            );

            if (failureOutcome.IsFailure)
            {
                logger.LogError("Failed to mark run as failed: {Fault}", failureOutcome.Fault.Detail);
                return;
            }

            bool paused = workflow.RecordRunFailure(utcNow);

            await PublishNotification
            (
                workflow: workflow,
                workflowRun: workflowRun,
                category: paused ? "WorkflowPaused" : "WorkflowFailed",
                bodyPreview: result.FailureMessage ?? "Workflow run failed",
                userId: workflow.UserId,
                cancellationToken: cancellationToken
            );

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task PublishNotification
    (
        Workflow workflow,
        WorkflowRun workflowRun,
        string category,
        string bodyPreview,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        string? emailAddress = await dbContext.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.EmailAddress)
            .FirstOrDefaultAsync(cancellationToken);

        WorkflowRunNotificationRequested notification = new()
        {
            EventId = Guid.NewGuid(),
            OccurredAt = dateTimeProvider.UtcNow,
            UserId = userId,
            WorkflowId = workflow.Id.Value,
            RunId = workflowRun.Id.Value,
            Category = category,
            Title = workflow.Title,
            BodyPreview = bodyPreview.Length > 200 ? bodyPreview[..200] : bodyPreview,
            RecipientEmailAddress = emailAddress ?? string.Empty
        };

        await messageBus.PublishAsync(notification, cancellationToken);
    }
}