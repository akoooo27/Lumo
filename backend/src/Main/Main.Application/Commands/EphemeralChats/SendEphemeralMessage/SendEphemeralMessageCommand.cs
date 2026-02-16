using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.EphemeralChats.SendEphemeralMessage;

public record SendEphemeralMessageCommand
(
    string EphemeralChatId,
    string Message
) : ICommand<SendEphemeralMessageResponse>, ISensitiveRequest;