using System.Text.Json.Serialization;

namespace Auth.Application.Abstractions.ZeroBounce;

public sealed record ZeroBounceValidateResponse
{
    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("sub_status")]
    public string? SubStatus { get; init; }

    [JsonPropertyName("account")]
    public string? Account { get; init; }

    [JsonPropertyName("domain")]
    public string? Domain { get; init; }

    [JsonPropertyName("free_email")]
    public bool? FreeEmail { get; init; }

    [JsonPropertyName("did_you_mean")]
    public string? DidYouMean { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}