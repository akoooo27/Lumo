namespace Main.Infrastructure.Caching;

public interface IFileCache
{
    Task<byte[]> GetOrSetAsync(string key, Func<Task<byte[]>> factory, CancellationToken cancellationToken = default);
}