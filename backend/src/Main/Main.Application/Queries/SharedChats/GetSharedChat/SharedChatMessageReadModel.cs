namespace Main.Application.Queries.SharedChats.GetSharedChat;

public sealed record SharedChatMessageReadModel
{
    public required int SequenceNumber { get; init; }

    public required string MessageRole { get; init; }

    public required string MessageContent { get; init; }

    public string? AttachmentFileKey { get; init; }

    public string? AttachmentContentType { get; init; }

    public long? AttachmentFileSizeInBytes { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset EditedAt { get; init; }
}