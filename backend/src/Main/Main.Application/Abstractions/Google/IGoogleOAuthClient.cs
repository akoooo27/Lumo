namespace Main.Application.Abstractions.Google;

public interface IGoogleOAuthClient
{
    Task<GoogleTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken);

    Task<GoogleTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeTokenAsync(string token, CancellationToken cancellationToken);

    Task<string> GetUserEmailAsync(string accessToken, CancellationToken cancellationToken);

    Uri BuildAuthorizationUrl(string state);
}