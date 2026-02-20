using System.ComponentModel.DataAnnotations;

namespace Main.Infrastructure.Options;

internal sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    [Required, MinLength(1)]
    public string ApiKey { get; init; } = string.Empty;

    [Required, Url]
    public string BaseUrl { get; init; } = "https://openrouter.ai/api/v1";

    [Required, MinLength(1)]
    public string DefaultModel { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string EmbeddingModel { get; init; } = string.Empty;

    public string? AppName { get; init; }

    [Url]
    public string? SiteUrl { get; init; }

    public List<ModelConfiguration> AllowedModels { get; init; } = [];
}

public sealed class ModelConfiguration
{
    [Required]
    public string Id { get; init; } = string.Empty;

    [Required]
    public string OpenRouterId { get; init; } = string.Empty;

    [Required]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    public string Provider { get; init; } = string.Empty;

    public bool IsDefault { get; init; }

    public int MaxContextTokens { get; init; } = 4096;

    public bool SupportsVision { get; init; }

    public bool SupportsStreaming { get; init; } = true;

    public bool SupportsFunctionCalling { get; init; } = true;
}