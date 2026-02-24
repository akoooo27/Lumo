using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Generators;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Preferences.EnableMemory;

internal sealed class EnableMemoryHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IIdGenerator idGenerator,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<EnableMemoryCommand>
{
    public async ValueTask<Outcome> Handle(EnableMemoryCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Preference? preference = await dbContext.Preferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        bool isNewPreference = false;

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
            isNewPreference = true;
        }

        if (!isNewPreference)
        {
            Outcome enableOutcome = preference.EnableMemory(dateTimeProvider.UtcNow);

            if (enableOutcome.IsFailure)
                return enableOutcome.Fault;
        }
        else
        {
            await dbContext.Preferences.AddAsync(preference, cancellationToken);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return PreferenceOperationFaults.Conflict;
        }

        return Outcome.Success();
    }
}