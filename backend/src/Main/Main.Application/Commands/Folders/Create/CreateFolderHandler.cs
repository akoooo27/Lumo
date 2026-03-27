using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Generators;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.Constants;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Folders.Create;

internal sealed class CreateFolderHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IIdGenerator idGenerator,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateFolderCommand, CreateFolderResponse>
{
    public async ValueTask<Outcome<CreateFolderResponse>> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        var stats = await dbContext.Folders
            .Where(f => f.UserId == userId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                MaxSortOrder = g.Max(f => (int?)f.SortOrder) ?? -1
            })
            .SingleOrDefaultAsync(cancellationToken);

        int folderCount = stats?.Count ?? 0;
        int nextSortOrder = (stats?.MaxSortOrder ?? -1) + 1;

        if (folderCount >= FolderConstants.MaxFoldersPerUser)
            return FolderOperationFaults.MaxFoldersReached;

        FolderId folderId = idGenerator.NewFolderId();

        DateTimeOffset utcNow = dateTimeProvider.UtcNow;

        Outcome<Folder> folderOutcome = Folder.Create
        (
            id: folderId,
            userId: userId,
            name: request.Name,
            sortOrder: nextSortOrder,
            utcNow: utcNow
        );

        if (folderOutcome.IsFailure)
            return folderOutcome.Fault;

        Folder folder = folderOutcome.Value;

        await dbContext.Folders.AddAsync(folder, cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return FolderOperationFaults.DuplicateName;
        }

        CreateFolderResponse response = new
        (
            FolderId: folder.Id.Value,
            Name: folder.Name,
            SortOrder: folder.SortOrder,
            CreatedAt: folder.CreatedAt
        );

        return response;
    }
}