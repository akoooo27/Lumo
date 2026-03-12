namespace Main.Infrastructure.AI;

internal sealed record ToolCallSource
(
    string Title,
    string Url,
    double Score
);