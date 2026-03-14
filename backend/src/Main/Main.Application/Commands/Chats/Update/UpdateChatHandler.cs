using Main.Application.Abstractions.Data;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.ReadModels;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Chats.Update;

internal sealed class UpdateChatHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateChatCommand, UpdateChatResponse>
{
    public async ValueTask<Outcome<UpdateChatResponse>> Handle(UpdateChatCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        User? user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user is null)
            return UserOperationFaults.NotFound;

        Outcome<ChatId> chatIdOutcome = ChatId.From(request.ChatId);

        if (chatIdOutcome.IsFailure)
            return chatIdOutcome.Fault;

        ChatId chatId = chatIdOutcome.Value;

        Chat? chat = await dbContext.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId, cancellationToken);

        if (chat is null)
            return ChatOperationFaults.NotFound;

        if (request.IsArchived is true)
        {
            Outcome archiveOutcome = chat.Archive(dateTimeProvider.UtcNow);

            if (archiveOutcome.IsFailure)
                return archiveOutcome.Fault;
        }
        else if (request.IsArchived is false)
        {
            Outcome unarchiveOutcome = chat.Unarchive(dateTimeProvider.UtcNow);

            if (unarchiveOutcome.IsFailure)
                return unarchiveOutcome.Fault;
        }

        if (request.IsPinned is true)
        {
            Outcome pinOutcome = chat.Pin(dateTimeProvider.UtcNow);

            if (pinOutcome.IsFailure)
                return pinOutcome.Fault;
        }
        else if (request.IsPinned is false)
        {
            Outcome unpinOutcome = chat.Unpin(dateTimeProvider.UtcNow);

            if (unpinOutcome.IsFailure)
                return unpinOutcome.Fault;
        }

        if (request.HasFolderId)
        {
            if (string.IsNullOrEmpty(request.FolderId))
            {
                Outcome removeOutcome = chat.RemoveFromFolder(dateTimeProvider.UtcNow);

                if (removeOutcome.IsFailure)
                    return removeOutcome.Fault;
            }
            else
            {
                Outcome<FolderId> folderIdOutcome = FolderId.From(request.FolderId);

                if (folderIdOutcome.IsFailure)
                    return folderIdOutcome.Fault;

                FolderId folderId = folderIdOutcome.Value;

                bool folderExists = await dbContext.Folders
                    .AnyAsync(f => f.Id == folderId && f.UserId == userId, cancellationToken);

                if (!folderExists)
                    return FolderOperationFaults.FolderNotOwned;

                Outcome setOutcome = chat.MoveToFolder(folderId, dateTimeProvider.UtcNow);

                if (setOutcome.IsFailure)
                    return setOutcome.Fault;
            }
        }

        if (request.NewTitle is not null)
        {
            Outcome titleOutcome = chat.RenameTitle(request.NewTitle, dateTimeProvider.UtcNow);

            if (titleOutcome.IsFailure)
                return titleOutcome.Fault;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        UpdateChatResponse response = new
        (
            ChatId: chat.Id.Value,
            Title: chat.Title,
            FolderId: chat.FolderId?.Value,
            IsArchived: chat.IsArchived,
            UpdatedAt: chat.UpdatedAt ?? dateTimeProvider.UtcNow,
            IsPinned: chat.IsPinned
        );

        return response;
    }
}