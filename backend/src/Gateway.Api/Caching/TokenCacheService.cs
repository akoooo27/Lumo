using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using SharedKernel.Application.Authentication;
using SharedKernel.Infrastructure.Options;

namespace Gateway.Api.Caching;

internal sealed class TokenCacheService(
    IDistributedCache distributedCache,
    ISecretHasher secretHasher,
    IOptions<JwtOptions> jwtOptions)
    : ITokenCacheService
{
    private const string AccessTokenKeyPrefix = "access_token_";

    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task SetAccessTokenAsync(string refreshToken, string accessToken, CancellationToken cancellationToken = default)
    {
        string hashedCacheKey = AccessTokenKeyPrefix + secretHasher.HashDeterministic(refreshToken);

        DistributedCacheEntryOptions entryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = _jwtOptions.AccessTokenExpiration
        };

        await distributedCache.SetStringAsync(hashedCacheKey, accessToken, entryOptions, cancellationToken);
    }

    public async Task<string?> GetAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        string hashedCacheKey = AccessTokenKeyPrefix + secretHasher.HashDeterministic(refreshToken);

        return await distributedCache.GetStringAsync(hashedCacheKey, cancellationToken);
    }

    public async Task RemoveAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        string hashedCacheKey = AccessTokenKeyPrefix + secretHasher.HashDeterministic(refreshToken);

        await distributedCache.RemoveAsync(hashedCacheKey, cancellationToken);
    }
}