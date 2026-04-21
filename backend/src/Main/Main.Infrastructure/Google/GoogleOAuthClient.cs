using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Services;

using Main.Application.Abstractions.Google;
using Main.Infrastructure.Options;

using Microsoft.Extensions.Options;

namespace Main.Infrastructure.Google;

internal sealed class GoogleOAuthClient(GoogleAuthorizationCodeFlow flow, IOptions<GoogleOptions> googleOptions)
    : IGoogleOAuthClient
{
    private readonly GoogleOptions _googleOptions = googleOptions.Value;

    public async Task<GoogleTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        TokenResponse tokenResponse = await flow.ExchangeCodeForTokenAsync
        (
            userId: string.Empty,
            code: code,
            redirectUri: _googleOptions.RedirectUri,
            taskCancellationToken: cancellationToken
        );

        return new GoogleTokenResponse
        (
            AccessToken: tokenResponse.AccessToken,
            RefreshToken: tokenResponse.RefreshToken,
            ExpiresInSeconds: (int)tokenResponse.ExpiresInSeconds.GetValueOrDefault(3600)
        );
    }

    public async Task<GoogleTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        TokenResponse token = await flow.RefreshTokenAsync
        (
            userId: string.Empty,
            refreshToken: refreshToken,
            taskCancellationToken: cancellationToken
        );

        return new GoogleTokenResponse
        (
            AccessToken: token.AccessToken,
            RefreshToken: token.RefreshToken,
            ExpiresInSeconds: (int)token.ExpiresInSeconds.GetValueOrDefault(3600)
        );
    }

    public async Task RevokeTokenAsync(string token, CancellationToken cancellationToken) =>
        await flow.RevokeTokenAsync
        (
            userId: string.Empty,
            token: token,
            taskCancellationToken: cancellationToken
        );

    public async Task<string> GetUserEmailAsync(string accessToken, CancellationToken cancellationToken)
    {
        GoogleCredential googleCredential = GoogleCredential.FromAccessToken(accessToken);

        using Oauth2Service oauth2 = new(new BaseClientService.Initializer
        {
            HttpClientInitializer = googleCredential
        });

        Userinfo userinfo = await oauth2.Userinfo.Get().ExecuteAsync(cancellationToken);

        return userinfo.Email;
    }

    public Uri BuildAuthorizationUrl(string state)
    {
        AuthorizationCodeRequestUrl request = flow.CreateAuthorizationCodeRequest(_googleOptions.RedirectUri);
        return new Uri($"{request.Build()}&state={Uri.EscapeDataString(state)}&access_type=offline&prompt=consent");
    }
}