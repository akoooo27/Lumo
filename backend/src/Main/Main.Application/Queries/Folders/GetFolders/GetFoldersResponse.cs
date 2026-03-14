namespace Main.Application.Queries.Folders.GetFolders;

public sealed record GetFoldersResponse(IReadOnlyList<FolderReadModel> Folders);