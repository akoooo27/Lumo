using Contracts.IntegrationEvents.Workflow;

using Main.Application.Abstractions.Generators;
using Main.Application.Abstractions.Memory;
using Main.Application.Abstractions.Workflows;
using Main.Domain.Aggregates;
using Main.Domain.Entities;
using Main.Domain.Enums;
using Main.Domain.ValueObjects;
using Main.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel;
using SharedKernel.Application.Messaging;

namespace Main.Infrastructure.Jobs;

internal sealed class CronJobHelper(
    MainDbContext dbContext,
    IIdGenerator idGenerator,
    ILogger<CronJobHelper> logger,
    IMessageBus messageBus,
    IWorkflowScheduleService workflowScheduleService,
    IDateTimeProvider dateTimeProvider) : ICronJobHelper
{
    public async Task PurgeDeletedMemoriesAsync(CancellationToken cancellationToken = default)
    {
        const int batchSize = 1000;
        DateTimeOffset cutoff = dateTimeProvider.UtcNow.AddDays(-MemoryConstants.StaleDaysThreshold);

        while (true)
        {
            List<string> idsToDelete = await dbContext.Memories
                .Where(m => !m.IsActive && m.UpdatedAt.HasValue && m.UpdatedAt.Value <= cutoff)
                .OrderBy(m => m.UpdatedAt)
                .Select(m => m.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (idsToDelete.Count == 0)
                return;

            await dbContext.Memories
                .Where(m => idsToDelete.Contains(m.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    public async Task DispatchDueWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset utcNow = dateTimeProvider.UtcNow;

        List<Workflow> dueWorkflows = await dbContext.Workflows
            .Include(w =>
                w.WorkflowRuns.Where(r =>
                    r.Status == WorkflowRunStatus.Queued || r.Status == WorkflowRunStatus.Running))
            .Where(w => w.Status == WorkflowStatus.Active && w.NextRunAt <= utcNow &&
                        (w.DispatchLeaseUntilUtc == null || w.DispatchLeaseUntilUtc <= utcNow))
            .ToListAsync(cancellationToken);

        foreach (Workflow workflow in dueWorkflows)
        {
            Guid leaseId = Guid.NewGuid();
            DateTimeOffset leaseUntil = utcNow.AddMinutes(5);

            bool dispatchLeaseClaimed = workflow.TryClaimDispatchLease
            (
                leaseId: leaseId,
                leaseUntilUtc: leaseUntil,
                utcNow: utcNow
            );

            if (!dispatchLeaseClaimed)
                continue;

            bool hasActiveRun = workflow.WorkflowRuns.Any(r =>
                r.Status == WorkflowRunStatus.Queued || r.Status == WorkflowRunStatus.Running);

            WorkflowRunId workflowRunId = idGenerator.NewWorkflowRunId();

            if (hasActiveRun)
            {
                Outcome<WorkflowRun> skipOutcome = workflow.CreateSkippedRun
                (
                    runId: workflowRunId,
                    scheduledFor: workflow.NextRunAt,
                    skipReason: "Previous run still in progress.",
                    utcNow: utcNow
                );

                if (skipOutcome.IsFailure)
                {
                    logger.LogError("Failed to create skipped run for workflow {WorkflowId}: {Fault}",
                        workflow.Id.Value, skipOutcome.Fault.Detail);
                    continue;
                }
            }
            else
            {
                Outcome<WorkflowRun> queueOutcome = workflow.CreateQueuedRun
                (
                    runId: workflowRunId,
                    scheduledFor: workflow.NextRunAt,
                    utcNow: utcNow
                );

                if (queueOutcome.IsFailure)
                {
                    logger.LogError("Failed to create queued run for workflow {WorkflowId}: {Fault}",
                        workflow.Id.Value, queueOutcome.Fault.Detail);
                    continue;
                }

                WorkflowRun workflowRun = queueOutcome.Value;

                WorkflowRunRequested workflowRunRequested = new()
                {
                    EventId = Guid.NewGuid(),
                    OccurredAt = utcNow,
                    WorkflowId = workflow.Id.Value,
                    RunId = workflowRun.Id.Value,
                    UserId = workflow.UserId,
                    ModelId = workflow.ModelId,
                    Instruction = workflow.Instruction,
                    UseWebSearch = workflow.UseWebSearch
                };

                await messageBus.PublishAsync(workflowRunRequested, cancellationToken);

                DateTimeOffset nextRunAt = workflowScheduleService.GetNextOccurrence
                (
                    kind: workflow.RecurrenceKind,
                    daysOfWeek: GetDaysFromMask(workflow.RecurrenceKind, workflow.DaysOfWeekMask),
                    localTime: workflow.LocalTime,
                    timeZoneId: workflow.TimeZoneId,
                    fromUtc: utcNow
                );

                workflow.AdvanceNextRunAt(nextRunAt, utcNow);
                workflow.ClearDispatchLease();
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<DayOfWeek>? GetDaysFromMask(WorkflowRecurrenceKind kind, int daysOfWeekMask)
    {
        if (kind != WorkflowRecurrenceKind.Weekly)
            return null;

        List<DayOfWeek> days = [];

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            if ((daysOfWeekMask & (1 << (int)day)) != 0)
                days.Add(day);
        }

        return days;
    }
}