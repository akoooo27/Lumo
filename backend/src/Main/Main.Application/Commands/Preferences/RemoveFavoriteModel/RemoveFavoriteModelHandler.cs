using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Services;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Preferences.RemoveFavoriteModel;

internal sealed class RemoveFavoriteModelHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IFavoriteModelsReadStore favoriteModelsReadStore,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RemoveFavoriteModelCommand>
{
    public async ValueTask<Outcome> Handle(RemoveFavoriteModelCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Outcome<FavoriteModelId> favoriteModelIdOutcome = FavoriteModelId.From(request.FavoriteModelId);

        if (favoriteModelIdOutcome.IsFailure)
            return favoriteModelIdOutcome.Fault;

        FavoriteModelId favoriteModelId = favoriteModelIdOutcome.Value;

        Preference? preference = await dbContext.Preferences
            .Include(p => p.FavoriteModels)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (preference is null)
            return PreferenceOperationFaults.NotFound;

        Outcome removeOutcome = preference.RemoveFavoriteModel
        (
            favoriteModelId: favoriteModelId,
            utcNow: dateTimeProvider.UtcNow
        );

        if (removeOutcome.IsFailure)
            return removeOutcome.Fault;

        await dbContext.SaveChangesAsync(cancellationToken);

        await favoriteModelsReadStore.InvalidateCacheAsync(userId, cancellationToken);

        return Outcome.Success();
    }
}