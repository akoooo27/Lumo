namespace Main.Application.Queries.Folders.GetFolders;

public sealed record FolderReadModel
{
    public required string FolderId { get; init; }

    public required string Name { get; init; }

    public required int SortOrder { get; init; }

    public required int ChatCount { get; init; }
}