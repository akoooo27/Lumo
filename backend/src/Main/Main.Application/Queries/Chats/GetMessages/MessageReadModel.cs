namespace Main.Application.Queries.Chats.GetMessages;

public sealed record MessageReadModel
{
    public required string Id { get; init; }

    public required string ChatId { get; init; }

    public required string MessageRole { get; init; }

    public required string MessageContent { get; init; }

    public long? InputTokenCount { get; init; }

    public long? OutputTokenCount { get; init; }

    public long? TotalTokenCount { get; init; }

    public int SequenceNumber { get; init; }

    public string? SourcesJson { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset EditedAt { get; init; }
}