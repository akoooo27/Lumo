using System.Data.Common;
using System.Text.Json;

using Auth.Application.Abstractions.Users;
using Auth.Application.Queries.Users.GetCurrentUser;

using Dapper;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using SharedKernel.Application.Data;

namespace Auth.Infrastructure.Users;

internal sealed class CurrentUserReadStore(
    IDbConnectionFactory dbConnectionFactory,
    IDistributedCache cache,
    ILogger<CurrentUserReadStore> logger) : ICurrentUserReadStore
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };
    private const string CacheKeyPrefix = "current-user:";

    private const string Sql = """
                               SELECT
                                    id as Id,
                                    display_name as DisplayName,
                                    email_address as EmailAddress,
                                    avatar_key as AvatarKey,
                                    created_at as CreatedAt
                               FROM users
                               where id = @UserId
                               """;

    public async Task<UserReadModel?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        string cacheKey = $"{CacheKeyPrefix}{userId}";

        string? cached = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (cached is not null)
        {
            try
            {
                return JsonSerializer.Deserialize<UserReadModel>(cached);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to deserialize cached user {UserId}, falling back to database", userId);
                await cache.RemoveAsync(cacheKey, cancellationToken);
            }
        }

        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        UserReadModel? user = await connection.QuerySingleOrDefaultAsync<UserReadModel>
        (
            Sql,
            new { UserId = userId }
        );

        if (user is null)
            return null;

        string serialized = JsonSerializer.Serialize(user);

        await cache.SetStringAsync
        (
            key: cacheKey,
            value: serialized,
            options: CacheOptions,
            token: cancellationToken
        );

        return user;
    }

    public async Task InvalidateCacheAsync(Guid userId, CancellationToken cancellationToken)
    {
        string cacheKey = $"{CacheKeyPrefix}{userId}";

        await cache.RemoveAsync(cacheKey, cancellationToken);
    }
}