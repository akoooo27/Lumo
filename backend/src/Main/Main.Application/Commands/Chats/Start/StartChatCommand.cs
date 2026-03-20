using Main.Application.Abstractions.Storage;

using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Chats.Start;

public sealed record StartChatCommand
(
    string Message,
    string? ModelId,
    bool WebSearchEnabled,
    AttachmentDto? AttachmentDto
) : ICommand<StartChatResponse>, ISensitiveRequest;