namespace Main.Api.Endpoints.Folders.Update;

internal sealed record Response
(
    string FolderId,
    string Name,
    int SortOrder,
    DateTimeOffset? UpdatedAt
);