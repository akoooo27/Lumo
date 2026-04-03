using Main.Domain.Enums;

namespace Main.Application.Abstractions.AI;

public sealed record ChatCompletionMessage
(
    MessageRole Role,
    string Content,
    string? AttachmentFileKey = null,
    string? AttachmentContentType = null
);