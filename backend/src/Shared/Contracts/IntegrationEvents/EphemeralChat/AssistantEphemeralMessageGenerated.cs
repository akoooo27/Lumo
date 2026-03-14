namespace Contracts.IntegrationEvents.EphemeralChat;

public sealed record AssistantEphemeralMessageGenerated
{
    public required Guid EventId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required Guid CorrelationId { get; init; }

    public required string EphemeralChatId { get; init; }

    public required string MessageContent { get; init; }

    public int? InputTokens { get; init; }

    public int? OutputTokens { get; init; }

    public int? TotalTokens { get; init; }

    public string? ModelId { get; init; }
}