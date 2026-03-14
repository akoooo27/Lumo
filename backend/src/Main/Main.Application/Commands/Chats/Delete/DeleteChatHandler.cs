using Main.Application.Abstractions.Data;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Chats.Delete;

internal sealed class DeleteChatHandler(IMainDbContext dbContext, IUserContext userContext)
    : ICommandHandler<DeleteChatCommand>
{
    public async ValueTask<Outcome> Handle(DeleteChatCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        bool userExists = await dbContext.Users
            .AnyAsync(u => u.UserId == userId, cancellationToken);

        if (!userExists)
            return UserOperationFaults.NotFound;

        Outcome<ChatId> chatIdOutcome = ChatId.From(request.ChatId);

        if (chatIdOutcome.IsFailure)
            return chatIdOutcome.Fault;

        ChatId chatId = chatIdOutcome.Value;

        Chat? chat = await dbContext.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId, cancellationToken);

        if (chat is null)
            return ChatOperationFaults.NotFound;

        dbContext.Chats.Remove(chat);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Outcome.Success();
    }
}