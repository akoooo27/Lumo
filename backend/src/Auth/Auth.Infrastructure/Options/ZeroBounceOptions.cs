using System.ComponentModel.DataAnnotations;

namespace Auth.Infrastructure.Options;

internal sealed class ZeroBounceOptions
{
    public const string SectionName = "ZeroBounce";

    [Required, MinLength(1)]
    public string ApiKey { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string BaseUrl { get; init; } = string.Empty;
}