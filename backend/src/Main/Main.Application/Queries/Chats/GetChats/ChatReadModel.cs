namespace Main.Application.Queries.Chats.GetChats;

public sealed record class ChatReadModel
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public string? ModelName { get; init; }

    public bool IsArchived { get; init; }

    public bool IsPinned { get; init; }

    public string? FolderId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public int MessagesCount { get; init; }
}