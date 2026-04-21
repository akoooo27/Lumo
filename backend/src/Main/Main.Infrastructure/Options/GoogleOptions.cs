using System.ComponentModel.DataAnnotations;

namespace Main.Infrastructure.Options;

internal sealed class GoogleOptions
{
    public const string SectionName = "Google";

    [Required, MinLength(1)]
    public string ClientId { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string ClientSecret { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string RedirectUri { get; init; } = string.Empty;
}