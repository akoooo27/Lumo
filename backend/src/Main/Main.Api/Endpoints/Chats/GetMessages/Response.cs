namespace Main.Api.Endpoints.Chats.GetMessages;

internal sealed record MessageDto
(
    string Id,
    string ChatId,
    string MessageRole,
    string MessageContent,
    long? InputTokenCount,
    long? OutputTokenCount,
    long? TotalTokenCount,
    int SequenceNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset EditedAt
);

internal sealed record PaginationDto
(
    int? NextCursor,
    bool HasMore,
    int Limit
);

internal sealed record Response(
    IReadOnlyList<MessageDto> Messages,
    PaginationDto Pagination
);