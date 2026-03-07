namespace Main.Api.Endpoints.Folders.Update;

internal sealed record Request
(
    string FolderId,
    string? NewName,
    int? SortOrder
);