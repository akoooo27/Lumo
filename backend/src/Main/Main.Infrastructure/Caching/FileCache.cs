using Microsoft.Extensions.Caching.Memory;

namespace Main.Infrastructure.Caching;

internal sealed class FileCache(IMemoryCache memoryCache) : IFileCache
{
    public async Task<byte[]> GetOrSetAsync
    (
        string key,
        Func<Task<byte[]>> factory,
        CancellationToken cancellationToken = default
    )
    {
        if (memoryCache.TryGetValue(key, out byte[]? cached))
            return cached!;

        byte[] bytes = await factory();

        memoryCache.Set
        (
            key: key,
            value: bytes,
            absoluteExpirationRelativeToNow: TimeSpan.FromDays(1)
        );

        return bytes;
    }
}