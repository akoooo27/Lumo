using System.Net.Http.Json;

using Main.Infrastructure.AI.Search.DTOs;
using Main.Infrastructure.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Main.Infrastructure.AI.Search;

internal sealed class TavilySearchService(
    HttpClient httpClient,
    IOptions<TavilyOptions> tavilyOptions,
    ILogger<TavilySearchService> logger) : IWebSearchService
{
    private readonly TavilyOptions _tavilyOptions = tavilyOptions.Value;

    public async Task<WebSearchResponse> SearchAsync(string query, string topic, CancellationToken cancellationToken = default)
    {
        TavilySearchRequest requestBody = new()
        {
            Query = query,
            Topic = topic,
            SearchDepth = _tavilyOptions.SearchDepth,
            MaxResults = _tavilyOptions.MaxResults,
            IncludeAnswer = false
        };

        using HttpRequestMessage request = new(HttpMethod.Post, $"{_tavilyOptions.BaseUrl}/search");
        request.Headers.Add("Authorization", $"Bearer {_tavilyOptions.ApiKey}");
        request.Content = JsonContent.Create(requestBody);

        HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Tavily search failed with status {StatusCode} for query: {Query}", response.StatusCode,
                query);
            return new WebSearchResponse([]);
        }

        TavilyApiResponse? tavilyApiResponse = await response.Content
            .ReadFromJsonAsync<TavilyApiResponse>(cancellationToken: cancellationToken);

        if (tavilyApiResponse?.Results is null)
            return new WebSearchResponse([]);

        List<WebSearchResult> results = tavilyApiResponse.Results
            .Select(r => new WebSearchResult
            (
                Title: r.Title ?? string.Empty,
                Url: r.Url,
                Content: r.Content ?? string.Empty,
                Score: r.Score,
                PublishedDate: r.PublishedDate
            ))
            .ToList();

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Tavily search returned {Count} results for query: {Query}",
                results.Count, query);

        return new WebSearchResponse(results);
    }
}