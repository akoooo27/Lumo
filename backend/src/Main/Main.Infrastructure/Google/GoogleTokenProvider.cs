using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Google;
using Main.Domain.Aggregates;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Security;

namespace Main.Infrastructure.Google;

internal sealed class GoogleTokenProvider(
    IMainDbContext dbContext,
    IGoogleOAuthClient googleOAuthClient,
    IDataProtectorWrapper dataProtectorWrapper,
    IDateTimeProvider dateTimeProvider) : IGoogleTokenProvider
{
    public async Task<string?> GetAccessTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        GoogleConnection? googleConnection = await dbContext.GoogleConnections
            .FirstOrDefaultAsync(gc => gc.UserId == userId, cancellationToken);

        if (googleConnection is null)
            return null;

        DateTimeOffset utcNow = dateTimeProvider.UtcNow;

        if (!googleConnection.IsTokenExpired(utcNow))
            return dataProtectorWrapper.Unprotect(googleConnection.ProtectedAccessToken);

        string refreshToken = dataProtectorWrapper.Unprotect(googleConnection.ProtectedRefreshToken);

        GoogleTokenResponse googleTokenResponse =
            await googleOAuthClient.RefreshTokenAsync(refreshToken, cancellationToken);

        string newProtectedAccessToken = dataProtectorWrapper.Protect(googleTokenResponse.AccessToken);
        string newProtectedRefreshToken = dataProtectorWrapper.Protect(googleTokenResponse.RefreshToken);
        DateTimeOffset newExpiresAt = utcNow.AddSeconds(googleTokenResponse.ExpiresInSeconds);

        googleConnection.UpdateTokens
        (
            protectedAccessToken: newProtectedAccessToken,
            protectedRefreshToken: newProtectedRefreshToken,
            utcNow: utcNow,
            expiresAt: newExpiresAt
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        return googleTokenResponse.AccessToken;
    }
}