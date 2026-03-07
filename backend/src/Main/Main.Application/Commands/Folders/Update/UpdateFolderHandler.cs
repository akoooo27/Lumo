using Main.Application.Abstractions.Data;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Folders.Update;

internal sealed class UpdateFolderHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateFolderCommand, UpdateFolderResponse>
{
    public async ValueTask<Outcome<UpdateFolderResponse>> Handle(UpdateFolderCommand request, CancellationToken cancellationToken)
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

        DateTimeOffset utcNow = dateTimeProvider.UtcNow;

        if (request.NewName is not null)
        {
            Outcome renameOutcome = folder.Rename(request.NewName, utcNow);

            if (renameOutcome.IsFailure)
                return renameOutcome.Fault;
        }

        if (request.SortOrder.HasValue)
        {
            Outcome sortOutcome = folder.SetSortOrder(request.SortOrder.Value, utcNow);

            if (sortOutcome.IsFailure)
                return sortOutcome.Fault;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        // Is little broad
        catch (DbUpdateException)
        {
            return FolderOperationFaults.DuplicateName;
        }

        UpdateFolderResponse response = new
        (
            FolderId: folder.Id.Value,
            Name: folder.Name,
            SortOrder: folder.SortOrder,
            UpdatedAt: folder.UpdatedAt
        );

        return response;
    }
}