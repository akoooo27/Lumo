using Main.Application.Abstractions.Services;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Preferences.GetFavoriteModels;

internal sealed class GetFavoriteModelsHandler(
    IFavoriteModelsReadStore favoriteModelsReadStore,
    IUserContext userContext) : IQueryHandler<GetFavoriteModelsQuery, GetFavoriteModelsResponse>
{
    public async ValueTask<Outcome<GetFavoriteModelsResponse>> Handle(GetFavoriteModelsQuery request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        IReadOnlyList<FavoriteModelReadModel> models = await favoriteModelsReadStore.GetForUserAsync(userId, cancellationToken);

        return new GetFavoriteModelsResponse(models);
    }
}