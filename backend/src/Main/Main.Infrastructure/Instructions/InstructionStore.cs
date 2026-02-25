using System.Text.Json;

using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Instructions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Main.Infrastructure.Instructions;

internal sealed class InstructionStore(IMainDbContext dbContext, IDistributedCache cache) : IInstructionStore
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };
    private const string CacheKeyPrefix = "user-instructions:";

    public async Task<IReadOnlyList<InstructionEntry>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        string cacheKey = $"{CacheKeyPrefix}{userId}";

        string? cached = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (cached is not null)
            return JsonSerializer.Deserialize<List<InstructionEntry>>(cached)!;

        List<InstructionEntry> entries = await dbContext.Preferences
            .Where(p => p.UserId == userId)
            .SelectMany(p => p.Instructions)
            .OrderBy(i => i.Priority)
            .Select(i => new InstructionEntry
            (
                Content: i.Content,
                Priority: i.Priority
            ))
            .ToListAsync(cancellationToken);

        string serialized = JsonSerializer.Serialize(entries);

        await cache.SetStringAsync
        (
            key: cacheKey,
            value: serialized,
            options: CacheOptions,
            token: cancellationToken
        );

        return entries;
    }

    public async Task InvalidateCacheAsync(Guid userId, CancellationToken cancellationToken)
    {
        string cacheKey = $"{CacheKeyPrefix}{userId}";

        await cache.RemoveAsync(cacheKey, cancellationToken);
    }
}