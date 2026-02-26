using Main.Application.Queries.Preferences.GetFavoriteModels;

namespace Main.Application.Abstractions.Services;

public interface IFavoriteModelsReadStore
{
    Task<IReadOnlyList<FavoriteModelReadModel>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task InvalidateCacheAsync(Guid userId, CancellationToken cancellationToken);
}