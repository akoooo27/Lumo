using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.RecoveryRequests.Complete;

public sealed record CompleteRecoveryCommand(string TokenKey) : ICommand<CompleteRecoveryResponse>, ISensitiveRequest;