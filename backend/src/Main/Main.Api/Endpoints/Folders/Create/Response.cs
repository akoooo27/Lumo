namespace Main.Api.Endpoints.Folders.Create;

internal sealed record Response
(
    string FolderId,
    string Name,
    int SortOrder,
    DateTimeOffset CreatedAt
);