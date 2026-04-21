using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.GoogleConnections.InitiateOAuth;

public record InitiateGoogleOAuthCommand : ICommand<InitiateGoogleOAuthResponse>;