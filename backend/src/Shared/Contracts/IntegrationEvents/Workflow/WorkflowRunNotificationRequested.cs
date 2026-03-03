namespace Contracts.IntegrationEvents.Workflow;

public class WorkflowRunNotificationRequested
{
    public required Guid EventId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required Guid UserId { get; init; }

    public required string WorkflowId { get; init; }

    public required string RunId { get; init; }

    public required string Category { get; init; }

    public required string Title { get; init; }

    public required string BodyPreview { get; init; }

    public required string RecipientEmailAddress { get; init; }
}