using System.Net.Http.Json;

using Auth.Application.Abstractions.ZeroBounce;
using Auth.Infrastructure.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.ZeroBounce;

internal sealed class ZeroBounceService(
    HttpClient httpClient,
    IOptions<ZeroBounceOptions> zeroBounceOptions,
    ILogger<ZeroBounceService> logger) : IEmailValidationService
{
    private static readonly HashSet<string> BlockedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "invalid",
        "spamtrap",
        "abuse",
        "do_not_mail"
    };

    public async Task<bool> IsSpamEmailAsync(string emailAddress, CancellationToken cancellationToken)
    {
        ZeroBounceOptions options = zeroBounceOptions.Value;

        string encodedEmail = Uri.EscapeDataString(emailAddress);

        using HttpRequestMessage request = new(HttpMethod.Get, $"v2/validate?api_key={options.ApiKey}&email={encodedEmail}");

        HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("ZeroBounce returned non-success status code: {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"ZeroBounce API returned {response.StatusCode}.");
        }

        ZeroBounceValidateResponse? validateResponse = await response.Content
            .ReadFromJsonAsync<ZeroBounceValidateResponse>(cancellationToken: cancellationToken);

        if (validateResponse?.Status is null)
        {
            logger.LogWarning("Failed to parse ZeroBounce response or status was null.");
            throw new HttpRequestException("ZeroBounce API returned an invalid response.");
        }

        return BlockedStatuses.Contains(validateResponse.Status);
    }
}