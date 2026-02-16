using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.EmailChangeRequests.Verify;

public sealed record VerifyEmailChangeCommand
(
    string RequestId,
    string OtpToken
) : ICommand<VerifyEmailChangeResponse>, ISensitiveRequest;