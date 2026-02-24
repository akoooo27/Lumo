using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.Sessions.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand, ISensitiveRequest;