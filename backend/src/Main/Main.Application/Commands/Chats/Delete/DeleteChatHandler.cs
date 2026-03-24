using Contracts.IntegrationEvents.Chat;

using Main.Application.Abstractions.Data;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Chats.Delete;

internal sealed class DeleteChatHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IMessageBus messageBus,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteChatCommand>
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
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId, cancellationToken);

        if (chat is null)
            return ChatOperationFaults.NotFound;

        List<string> attachmentFileKeys =
        [
            .. chat.Messages
                .Where(m => m.Attachment is not null)
                .Select(m => m.Attachment!.FileKey)
        ];

        dbContext.Chats.Remove(chat);

        if (attachmentFileKeys.Count > 0)
        {
            ChatDeleted chatDeleted = new()
            {
                EventId = Guid.NewGuid(),
                OccurredAt = dateTimeProvider.UtcNow,
                CorrelationId = Guid.NewGuid(),
                ChatId = chatId.Value,
                UserId = userId,
                AttachmentFileKeys = attachmentFileKeys
            };

            await messageBus.PublishAsync(chatDeleted, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Outcome.Success();
    }
}