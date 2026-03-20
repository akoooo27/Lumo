using Main.Application.Abstractions.Storage;

using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Chats.SendMessage;

public sealed record SendMessageCommand
(
    string ChatId,
    string Message,
    bool WebSearchEnabled,
    AttachmentDto? AttachmentDto
) : ICommand<SendMessageResponse>, ISensitiveRequest;