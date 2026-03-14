namespace Main.Application.Commands.Folders.Update;

public sealed record UpdateFolderResponse
(
    string FolderId,
    string Name,
    int SortOrder,
    DateTimeOffset UpdatedAt
);