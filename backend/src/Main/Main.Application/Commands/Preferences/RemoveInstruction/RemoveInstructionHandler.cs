using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Instructions;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Preferences.RemoveInstruction;

internal sealed class RemoveInstructionHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IInstructionStore instructionStore,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RemoveInstructionCommand>
{
    public async ValueTask<Outcome> Handle(RemoveInstructionCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Outcome<InstructionId> instructionIdOutcome = InstructionId.From(request.InstructionId);

        if (instructionIdOutcome.IsFailure)
            return instructionIdOutcome.Fault;

        InstructionId instructionId = instructionIdOutcome.Value;

        Preference? preference = await dbContext.Preferences
            .Include(p => p.Instructions)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (preference is null)
            return PreferenceOperationFaults.NotFound;

        Outcome removeOutcome = preference.RemoveInstruction
        (
            instructionId: instructionId,
            utcNow: dateTimeProvider.UtcNow
        );

        if (removeOutcome.IsFailure)
            return removeOutcome.Fault;

        await dbContext.SaveChangesAsync(cancellationToken);

        await instructionStore.InvalidateCacheAsync(userId, cancellationToken);

        return Outcome.Success();
    }
}