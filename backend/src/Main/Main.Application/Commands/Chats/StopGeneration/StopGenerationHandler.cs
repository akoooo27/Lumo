using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Stream;
using Main.Application.Faults;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Chats.StopGeneration;

internal sealed class StopGenerationHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IChatLockService chatLockService) : ICommandHandler<StopGenerationCommand>
{
    public async ValueTask<Outcome> Handle(StopGenerationCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Outcome<ChatId> chatIdOutcome = ChatId.From(request.ChatId);

        if (chatIdOutcome.IsFailure)
            return chatIdOutcome.Fault;

        ChatId chatId = chatIdOutcome.Value;

        Outcome<StreamId> streamIdOutcome = StreamId.From(request.StreamId);

        if (streamIdOutcome.IsFailure)
            return streamIdOutcome.Fault;

        StreamId streamId = streamIdOutcome.Value;

        bool chatExists = await dbContext.Chats
            .AnyAsync(c => c.Id == chatId && c.UserId == userId, cancellationToken);

        if (!chatExists)
            return ChatOperationFaults.NotFound;

        bool isGenerating = await chatLockService.IsGeneratingAsync(chatId.Value, cancellationToken);

        if (!isGenerating)
            return ChatOperationFaults.NotGenerating;

        await chatLockService.RequestCancellationAsync(streamId.Value, cancellationToken);

        return Outcome.Success();
    }
}
