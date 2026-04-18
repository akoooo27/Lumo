namespace Main.Application.Abstractions.Google;

public sealed record GoogleTokenResponse
(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds
);