namespace Main.Api.Endpoints.Chats.Update;

internal sealed record Response
(
    string ChatId,
    string Title,
    string? FolderId,
    bool IsArchived,
    bool IsPinned,
    DateTimeOffset UpdatedAt
);