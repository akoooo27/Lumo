using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Chats.Start;

public sealed record StartChatCommand
(
    string Message,
    string? ModelId,
    bool WebSearchEnabled
) : ICommand<StartChatResponse>, ISensitiveRequest;