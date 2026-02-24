using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.RecoveryRequests.VerifyNewEmail;

public sealed record VerifyNewEmailCommand
(
    string TokenKey,
    string? OtpToken,
    string? MagicLinkToken
) : ICommand, ISensitiveRequest;