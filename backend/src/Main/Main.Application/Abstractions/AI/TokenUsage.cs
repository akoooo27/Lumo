namespace Main.Application.Abstractions.AI;

public sealed record TokenUsage
(
    int InputTokens,
    int OutputTokens,
    int TotalTokens
)
{
    public static readonly TokenUsage Empty = new(0, 0, 0);
}