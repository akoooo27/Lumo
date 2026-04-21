using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.GoogleConnections.HandleCallback;

public sealed record HandleGoogleCallbackCommand
(
    string Code,
    string State
) : ICommand;