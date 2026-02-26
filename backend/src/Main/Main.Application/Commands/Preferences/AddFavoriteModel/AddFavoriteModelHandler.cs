using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Generators;
using Main.Application.Abstractions.Services;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.Entities;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Preferences.AddFavoriteModel;

internal sealed class AddFavoriteModelHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IIdGenerator idGenerator,
    IModelRegistry modelRegistry,
    IFavoriteModelsReadStore favoriteModelsReadStore,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AddFavoriteModelCommand, AddFavoriteModelResponse>
{
    public async ValueTask<Outcome<AddFavoriteModelResponse>> Handle(AddFavoriteModelCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        ModelInfo? modelInfo = modelRegistry.GetModelInfo(request.ModelId);

        if (modelInfo is null)
            return PreferenceOperationFaults.ModelNotFound;

        Preference? preference = await dbContext.Preferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (preference is null)
        {
            PreferenceId preferenceId = idGenerator.NewPreferenceId();

            Outcome<Preference> preferenceOutcome = Preference.Create
            (
                id: preferenceId,
                userId: userId,
                utcNow: dateTimeProvider.UtcNow
            );

            if (preferenceOutcome.IsFailure)
                return preferenceOutcome.Fault;

            preference = preferenceOutcome.Value;

            await dbContext.Preferences.AddAsync(preference, cancellationToken);
        }

        FavoriteModelId favoriteModelId = idGenerator.NewFavoriteModelId();

        Outcome<FavoriteModel> addFavoriteModelOutcome = preference.AddFavoriteModel
        (
            favoriteModelId: favoriteModelId,
            modelId: request.ModelId,
            utcNow: dateTimeProvider.UtcNow
        );

        if (addFavoriteModelOutcome.IsFailure)
            return addFavoriteModelOutcome.Fault;

        FavoriteModel favoriteModel = addFavoriteModelOutcome.Value;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return PreferenceOperationFaults.Conflict;
        }

        await favoriteModelsReadStore.InvalidateCacheAsync(userId, cancellationToken);

        AddFavoriteModelResponse response = new
        (
            PreferenceId: preference.Id.Value,
            FavoriteModelId: favoriteModel.Id.Value,
            CreatedAt: favoriteModel.CreatedAt
        );

        return response;
    }
}