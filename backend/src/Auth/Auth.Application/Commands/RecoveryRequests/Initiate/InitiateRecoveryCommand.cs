using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.RecoveryRequests.Initiate;

public sealed record InitiateRecoveryCommand
(
    string RecoveryKey,
    string NewEmailAddress
) : ICommand<InitiateRecoveryResponse>, ISensitiveRequest;