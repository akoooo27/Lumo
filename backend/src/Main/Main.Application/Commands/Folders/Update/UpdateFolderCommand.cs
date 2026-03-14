using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Folders.Update;

public sealed record UpdateFolderCommand
(
    string FolderId,
    string? NewName,
    int? SortOrder
) : ICommand<UpdateFolderResponse>;