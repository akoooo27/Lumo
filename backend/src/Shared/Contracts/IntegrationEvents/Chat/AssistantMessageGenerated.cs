namespace Contracts.IntegrationEvents.Chat;

public sealed record AssistantMessageGenerated
{
    public required Guid EventId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required Guid CorrelationId { get; init; }

    public required string ChatId { get; init; }

    public required string MessageContent { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }

    public int? TotalTokens { get; init; }

    public string? ModelId { get; init; }

    public string? SourcesJson { get; init; }
}