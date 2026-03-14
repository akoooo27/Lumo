namespace Main.Api.Endpoints.Chats.Update;

internal sealed record Request
(
    string ChatId,
    string? NewTitle,
    bool? IsArchived,
    bool? IsPinned,
    string? FolderId
);