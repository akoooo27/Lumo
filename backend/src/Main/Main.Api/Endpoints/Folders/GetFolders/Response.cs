namespace Main.Api.Endpoints.Folders.GetFolders;

internal sealed record FolderDto
(
    string FolderId,
    string Name,
    int SortOrder,
    int ChatCount
);

internal sealed record Response
(
    IReadOnlyList<FolderDto> Folders
);