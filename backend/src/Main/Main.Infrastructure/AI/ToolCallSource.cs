namespace Main.Infrastructure.AI;

internal sealed record ToolCallSource
(
    string Title,
    string Url,
    double Score,
    string? PublishedDate
)
{
    public string Confidence => Score switch
    {
        >= 0.8 => "high",
        >= 0.5 => "moderate",
        _ => "low"
    };
}