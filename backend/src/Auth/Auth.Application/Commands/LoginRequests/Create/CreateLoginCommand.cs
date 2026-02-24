using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.LoginRequests.Create;

public sealed record CreateLoginCommand(string EmailAddress) : ICommand<CreateLoginResponse>, ISensitiveRequest;