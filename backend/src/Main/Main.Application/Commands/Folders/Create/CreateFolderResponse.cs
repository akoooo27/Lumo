namespace Main.Application.Commands.Folders.Create;

public sealed record CreateFolderResponse
(
    string FolderId,
    string Name,
    int SortOrder,
    DateTimeOffset CreatedAt
);