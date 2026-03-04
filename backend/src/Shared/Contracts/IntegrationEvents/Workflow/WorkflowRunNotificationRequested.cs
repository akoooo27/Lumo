namespace Contracts.IntegrationEvents.Workflow;

public class WorkflowRunNotificationRequested
{
    public required Guid EventId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required Guid UserId { get; init; }

    public required string WorkflowId { get; init; }

    public required string WorkflowRunId { get; init; }

    public required Guid IdempotencyId { get; init; }

    public required WorkflowNotificationCategory Category { get; init; }

    public required string Title { get; init; }

    public required string BodyPreview { get; init; }

    public required string RecipientEmailAddress { get; init; }

    public DateTimeOffset? NextRunAt { get; init; }
}