using System.Data.Common;
using System.Text.Json;

using Dapper;

using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Services;
using Main.Application.Queries.Preferences.GetFavoriteModels;

using Microsoft.Extensions.Caching.Distributed;

using SharedKernel.Application.Data;

namespace Main.Infrastructure.Preferences;

internal sealed class FavoriteModelsReadStore(
    IDbConnectionFactory dbConnectionFactory,
    IDistributedCache cache,
    IModelRegistry modelRegistry) : IFavoriteModelsReadStore
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };
    private const string CacheKeyPrefix = "favorite-models:";

    private const string Sql = """
                               SELECT
                                    fm.Id as Id,
                                    fm.model_id as ModelId,
                                    fm.created_at as CreatedAt
                               FROM favorite_models fm
                               INNER JOIN preferences p ON p.id = fm.preference_id
                               WHERE p.user_id = @UserId
                               ORDER BY fm.created_at DESC
                               """;

    public async Task<IReadOnlyList<FavoriteModelReadModel>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        string cacheKey = $"{CacheKeyPrefix}{userId}";

        string? cached = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (cached is not null)
            return JsonSerializer.Deserialize<List<FavoriteModelReadModel>>(cached)!;

        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        IEnumerable<FavoriteModelDbRow> favorites = await connection.QueryAsync<FavoriteModelDbRow>
        (
            Sql,
            new { UserId = userId }
        );

        List<FavoriteModelReadModel> models = favorites
            .Select(f =>
            {
                ModelInfo? info = modelRegistry.GetModelInfo(f.ModelId);

                return new FavoriteModelReadModel
                {
                    Id = f.Id,
                    ModelId = f.ModelId,
                    DisplayName = info?.DisplayName ?? f.ModelId,
                    Provider = info?.Provider ?? "Unknown Provider",
                    IsDefault = info?.IsDefault ?? false,
                    MaxContextTokens = info?.ModelCapabilities.MaxContextTokens ?? 0,
                    SupportsVision = info?.ModelCapabilities.SupportsVision ?? false,
                    SupportsStreaming = info?.ModelCapabilities.SupportsStreaming ?? false,
                    CreatedAt = f.CreatedAt
                };
            })
            .ToList();

        string serialized = JsonSerializer.Serialize(models);

        await cache.SetStringAsync
        (
            key: cacheKey,
            value: serialized,
            options: CacheOptions,
            token: cancellationToken
        );

        return models;
    }

    public async Task InvalidateCacheAsync(Guid userId, CancellationToken cancellationToken)
    {
        string cacheKey = $"{CacheKeyPrefix}{userId}";

        await cache.RemoveAsync(cacheKey, cancellationToken);
    }
}