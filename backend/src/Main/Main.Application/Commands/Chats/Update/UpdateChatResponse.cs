namespace Main.Application.Commands.Chats.Update;

public sealed record UpdateChatResponse
(
    string ChatId,
    string Title,
    string? FolderId,
    bool IsArchived,
    bool IsPinned,
    DateTimeOffset UpdatedAt
);