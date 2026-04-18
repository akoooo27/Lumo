using Main.Application.Abstractions.Ephemeral;
using Main.Application.Abstractions.Stream;
using Main.Application.Faults;
using Main.Domain.Models;
using Main.Domain.ValueObjects;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.EphemeralChats.StopGeneration;

internal sealed class StopEphemeralGenerationHandler(
    IUserContext userContext,
    IEphemeralChatStore ephemeralChatStore,
    IChatLockService chatLockService) : ICommandHandler<StopEphemeralGenerationCommand>
{
    public async ValueTask<Outcome> Handle(StopEphemeralGenerationCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Outcome<StreamId> streamIdOutcome = StreamId.From(request.StreamId);

        if (streamIdOutcome.IsFailure)
            return streamIdOutcome.Fault;

        StreamId streamId = streamIdOutcome.Value;

        EphemeralChat? ephemeralChat = await ephemeralChatStore.GetAsync(request.EphemeralChatId, cancellationToken);

        if (ephemeralChat is null || ephemeralChat.UserId != userId)
            return EphemeralChatOperationFaults.NotFound;

        bool isGenerating = await chatLockService.IsGeneratingAsync(ephemeralChat.EphemeralChatId, cancellationToken);

        if (!isGenerating)
            return EphemeralChatOperationFaults.NotGenerating;

        await chatLockService.RequestCancellationAsync(streamId.Value, cancellationToken);

        return Outcome.Success();
    }
}