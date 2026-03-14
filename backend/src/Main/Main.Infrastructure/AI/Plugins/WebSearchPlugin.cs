using System.ComponentModel;
using System.Globalization;
using System.Text;

using Main.Infrastructure.AI.Search;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Main.Infrastructure.AI.Plugins;

internal sealed class WebSearchPlugin
(
    IWebSearchService webSearchService,
    PluginStreamContext pluginStreamContext,
    ILogger<WebSearchPlugin> logger
)
{
    [KernelFunction("__ws")]
    [Description
        (
        "Search the web for current information. " +
        "Use this when the user asks about current events, recent news, real-time data, " +
        "or topics that may have changed after your knowledge cutoff. " +
        "Do NOT use for general knowledge, creative writing, math, or coding questions."
        )
    ]
    public async Task<string> SearchAsync
    (
        [Description("The search query to find current information on the web.")]
        string query,
        [Description("The type of search: 'general' for broad searches, 'news' for recent news. Defaults to 'general'.")]
        string topic = "general",
        CancellationToken cancellationToken = default
    )
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("WebSearchPlugin.SearchAsync called: Query={Query}, Topic={Topic}", query, topic);

        try
        {
            WebSearchResponse response = await webSearchService.SearchAsync(query, topic, cancellationToken);

            if (response.Results.Count == 0)
                return "No search results found.";

            pluginStreamContext.LastSearchSources = response.Results
                .Select(r => new ToolCallSource
                (
                    Title: r.Title,
                    Url: r.Url,
                    Score: r.Score,
                    PublishedDate: r.PublishedDate
                ))
                .ToList();

            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"Found {response.Results.Count} results:");
            stringBuilder.AppendLine();

            foreach (WebSearchResult result in response.Results)
            {
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"**{result.Title}**");
                stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"Source: {result.Url}");
                stringBuilder.AppendLine(result.Content);
                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Web search request failed");
            return "Web search is temporarily unavailable. Please try again later.";
        }
    }
}