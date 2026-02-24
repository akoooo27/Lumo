using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.EmailChangeRequests.Request;

public sealed record RequestEmailChangeCommand(string NewEmailAddress) : ICommand<RequestEmailChangeResponse>, ISensitiveRequest;