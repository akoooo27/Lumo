using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.EphemeralChats.Start;

public sealed record StartEphemeralChatCommand
(
    string Message,
    string? ModelId
) : ICommand<StartEphemeralChatResponse>, ISensitiveRequest;