namespace Main.Application.Abstractions.Stream;

public sealed record StreamMessage
(
    StreamMessageType Type,
    string Content,
    DateTimeOffset Timestamp,
    string? ModelName,
    string? Provider
)
{
    public string? Query { get; init; }

    public string? Sources { get; init; }

    public string? Memories { get; init; }
}