using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.Sessions.RefreshToken;

public sealed record RefreshTokenCommand
(
    string RefreshToken
) : ICommand<RefreshTokenResponse>, ISensitiveRequest;