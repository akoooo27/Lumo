namespace Main.Application.Abstractions.Services;

public interface IUserPreferenceResolver
{
    Task<bool> IsMemoryEnabledAsync(Guid userId, CancellationToken cancellationToken);

    Task InvalidateCacheAsync(Guid userId, CancellationToken cancellationToken);
}