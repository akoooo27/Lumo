using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.SharedChats;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.SharedChats.RefreshChat;

internal sealed class RefreshSharedChatHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    ISharedChatReadStore sharedChatReadStore,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RefreshSharedChatCommand, RefreshSharedChatResponse>
{
    public async ValueTask<Outcome<RefreshSharedChatResponse>> Handle(RefreshSharedChatCommand request,
        CancellationToken cancellationToken)
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

        Chat? sourceChat = await dbContext.Chats
            .FirstOrDefaultAsync(c => c.Id == sharedChat.SourceChatId, cancellationToken);

        if (sourceChat is null)
            return SharedChatOperationFaults.NotFound;

        IReadOnlyList<SharedChatMessage> refreshedMessages = sourceChat.Messages
            .OrderBy(m => m.SequenceNumber)
            .Select(m => new SharedChatMessage(
                SequenceNumber: m.SequenceNumber,
                MessageRole: m.MessageRole,
                MessageContent: m.MessageContent,
                CreatedAt: m.CreatedAt,
                EditedAt: m.EditedAt
            ))
            .ToList();

        sharedChat.RefreshMessages(refreshedMessages, dateTimeProvider.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        await sharedChatReadStore.InvalidateCacheAsync(request.SharedChatId, cancellationToken);

        RefreshSharedChatResponse response = new
        (
            SharedChatId: sharedChat.Id.Value,
            SnapshotAt: sharedChat.SnapshotAt,
            CreatedAt: sharedChat.CreatedAt
        );

        return response;
    }
}