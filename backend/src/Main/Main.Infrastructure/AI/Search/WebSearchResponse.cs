namespace Main.Infrastructure.AI.Search;

internal sealed record WebSearchResponse(IReadOnlyList<WebSearchResult> Results);

internal sealed record WebSearchResult
(
    string Title,
    string Url,
    string Content,
    double Score,
    string? PublishedDate
);