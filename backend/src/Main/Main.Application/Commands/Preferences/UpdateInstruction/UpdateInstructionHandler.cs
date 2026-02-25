using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Instructions;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.Entities;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Preferences.UpdateInstruction;

internal sealed class UpdateInstructionHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IInstructionStore instructionStore,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateInstructionCommand, UpdateInstructionResponse>
{
    public async ValueTask<Outcome<UpdateInstructionResponse>> Handle(UpdateInstructionCommand request, CancellationToken cancellationToken)
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

        Outcome updateOutcome = preference.UpdateInstruction
        (
            instructionId: instructionId,
            newContent: request.NewContent,
            utcNow: dateTimeProvider.UtcNow
        );

        if (updateOutcome.IsFailure)
            return updateOutcome.Fault;

        await dbContext.SaveChangesAsync(cancellationToken);

        await instructionStore.InvalidateCacheAsync(userId, cancellationToken);

        Instruction instruction = preference.Instructions.First(i => i.Id == instructionId);

        UpdateInstructionResponse response = new
        (
            InstructionId: instruction.Id.Value,
            Content: instruction.Content,
            Priority: instruction.Priority,
            UpdatedAt: instruction.UpdatedAt
        );

        return response;
    }
}