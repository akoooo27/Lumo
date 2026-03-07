using Main.Application.Abstractions.Data;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Folders.Delete;

internal sealed class DeleteFolderHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteFolderCommand>
{
    public async ValueTask<Outcome> Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Outcome<FolderId> folderIdOutcome = FolderId.From(request.FolderId);

        if (folderIdOutcome.IsFailure)
            return folderIdOutcome.Fault;

        FolderId folderId = folderIdOutcome.Value;

        Folder? folder = await dbContext.Folders
            .FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId, cancellationToken);

        if (folder is null)
            return FolderOperationFaults.NotFound;

        List<Chat> chats = await dbContext.Chats
            .Where(c => c.UserId == userId && c.FolderId == folderId)
            .ToListAsync(cancellationToken);

        DateTimeOffset utcNow = dateTimeProvider.UtcNow;

        foreach (Chat chat in chats)
        {
            chat.RemoveFromFolder(utcNow);
        }

        dbContext.Folders.Remove(folder);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Outcome.Success();
    }
}