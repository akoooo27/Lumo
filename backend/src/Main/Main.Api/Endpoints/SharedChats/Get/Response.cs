namespace Main.Api.Endpoints.SharedChats.Get;

internal sealed record SharedChatDto
(
    string Id,
    string SourceChatId,
    Guid OwnerId,
    string Title,
    string ModelId,
    int ViewCount,
    DateTimeOffset SnapshotAt,
    DateTimeOffset CreatedAt
);

internal sealed record SharedChatMessageDto
(
    int SequenceNumber,
    string MessageRole,
    string MessageContent,
    string? AttachmentFileKey,
    string? AttachmentContentType,
    long? AttachmentFileSizeInBytes,
    DateTimeOffset CreatedAt,
    DateTimeOffset EditedAt
);

internal sealed record Response
(
    SharedChatDto SharedChat,
    IReadOnlyList<SharedChatMessageDto> Messages
);