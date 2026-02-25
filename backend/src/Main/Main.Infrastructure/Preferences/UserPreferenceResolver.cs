using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Services;
using Main.Domain.Aggregates;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Main.Infrastructure.Preferences;

internal sealed class UserPreferenceResolver(IMainDbContext dbContext, IDistributedCache cache) : IUserPreferenceResolver
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };
    private const string CacheKeyPrefix = "user-preference:";

    public async Task<bool> IsMemoryEnabledAsync(Guid userId, CancellationToken cancellationToken)
    {
        string cacheKey = $"{CacheKeyPrefix}{userId}";

        string? cached = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (cached is not null)
            return cached == "1";

        Preference? preference = await dbContext.Preferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        bool result = preference?.MemoryEnabled ?? true;

        await cache.SetStringAsync
        (
            key: cacheKey,
            value: result ? "1" : "0",
            options: CacheOptions,
            token: cancellationToken
        );

        return result;
    }

    public async Task InvalidateCacheAsync(Guid userId, CancellationToken cancellationToken)
    {
        string cacheKey = $"{CacheKeyPrefix}{userId}";

        await cache.RemoveAsync(cacheKey, cancellationToken);
    }
}