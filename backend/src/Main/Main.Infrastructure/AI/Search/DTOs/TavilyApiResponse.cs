using System.Text.Json.Serialization;

namespace Main.Infrastructure.AI.Search.DTOs;

internal sealed record TavilyApiResponse
{
    [JsonPropertyName("results")]
    public required List<TavilyApiResponseItem> Results { get; init; }
}

internal sealed record TavilyApiResponseItem
{
    [JsonPropertyName("title")]
    public required string? Title { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("score")]
    public double Score { get; init; }

    [JsonPropertyName("published_date")]
    public string? PublishedDate { get; init; }
}