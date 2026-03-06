using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.SharedChats;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.SharedChats.DeleteSharedChat;

internal sealed class DeleteSharedChatHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    ISharedChatReadStore sharedChatReadStore)
    : ICommandHandler<DeleteSharedChatCommand>
{
    public async ValueTask<Outcome> Handle(DeleteSharedChatCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Outcome<SharedChatId> sharedChatIdOutcome = SharedChatId.From(request.SharedChatId);

        if (sharedChatIdOutcome.IsFailure)
            return sharedChatIdOutcome.Fault;

        SharedChatId sharedChatId = sharedChatIdOutcome.Value;

        SharedChat? sharedChat = await dbContext.SharedChats
            .FirstOrDefaultAsync(sc => sc.Id == sharedChatId && sc.OwnerId == userId, cancellationToken);

        if (sharedChat is null)
            return SharedChatOperationFaults.NotFound;

        dbContext.SharedChats.Remove(sharedChat);
        await dbContext.SaveChangesAsync(cancellationToken);

        await sharedChatReadStore.InvalidateCacheAsync(request.SharedChatId, cancellationToken);

        return Outcome.Success();
    }
}