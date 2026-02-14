using Main.Application.Abstractions.Ephemeral;
using Main.Application.Faults;
using Main.Domain.Models;
using Main.Domain.ValueObjects;

using Microsoft.Extensions.Logging;

using SharedKernel;
using SharedKernel.Application.Authentication;

namespace Main.Application.Abstractions.Services;

internal sealed class EphemeralChatAccessValidator(
    IEphemeralChatStore ephemeralChatStore,
    IUserContext userContext,
    ILogger<EphemeralChatAccessValidator> logger) : IEphemeralChatAccessValidator
{
    public async Task<Outcome> ValidateAccessAsync(string ephemeralChatId, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Validating access to ephemeral chat {EphemeralChatId} for user {UserId}",
                ephemeralChatId, userContext.UserId);

        Outcome<EphemeralChatId> chatIdOutcome = EphemeralChatId.From(ephemeralChatId);

        if (chatIdOutcome.IsFailure)
            return chatIdOutcome.Fault;

        EphemeralChat? ephemeralChat = await ephemeralChatStore.GetAsync(chatIdOutcome.Value.Value, cancellationToken);

        if (ephemeralChat is null || ephemeralChat.UserId != userContext.UserId)
            return EphemeralChatOperationFaults.NotFound;

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Access to ephemeral chat {EphemeralChatId} for user {UserId} validated successfully",
                ephemeralChatId, userContext.UserId);

        return Outcome.Success();
    }
}