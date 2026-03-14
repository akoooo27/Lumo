namespace Main.Application.Queries.Chats.SearchChats;

public sealed record SearchChatReadModel
{
    public required string ChatId { get; init; }

    public required string Title { get; init; }

    public string? ModelName { get; init; }

    public string? Folder { get; init; }

    public required string Snippet { get; init; }

    public required DateTimeOffset MatchedAt { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}