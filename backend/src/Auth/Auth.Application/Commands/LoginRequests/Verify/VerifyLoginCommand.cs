using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.LoginRequests.Verify;

public sealed record VerifyLoginCommand
(
    string TokenKey,
    string? OtpToken,
    string? MagicLinkToken
) : ICommand<VerifyLoginResponse>, ISensitiveRequest;