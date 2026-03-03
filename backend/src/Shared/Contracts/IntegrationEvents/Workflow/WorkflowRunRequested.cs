namespace Contracts.IntegrationEvents.Workflow;

public sealed record WorkflowRunRequested
{
    public required Guid EventId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required string WorkflowId { get; init; }

    public required string RunId { get; init; }

    public required Guid UserId { get; init; }

    public required string ModelId { get; init; }

    public required string Instruction { get; init; }

    public required bool UseWebSearch { get; init; }
}