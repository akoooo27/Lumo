using Auth.Application.Abstractions.Authentication;
using Auth.Application.Abstractions.Data;
using Auth.Application.Faults;
using Auth.Domain.Aggregates;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.RecoveryRequests.VerifyNewEmail;

internal sealed class VerifyNewEmailCommandHandler(
    IAuthDbContext dbContext,
    ISecureTokenGenerator secureTokenGenerator,
    IAttemptTracker attemptTracker,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<VerifyNewEmailCommand>
{
    public async ValueTask<Outcome> Handle(VerifyNewEmailCommand request, CancellationToken cancellationToken)
    {
        bool isLocked = await attemptTracker.IsLockedAsync(request.TokenKey, cancellationToken);

        if (isLocked)
            return RecoveryRequestOperationFaults.TooManyAttempts;

        RecoveryRequest? recoveryRequest = await dbContext.RecoveryRequests
            .FirstOrDefaultAsync(rr => rr.TokenKey == request.TokenKey, cancellationToken);

        if (recoveryRequest is null)
            return RecoveryRequestOperationFaults.InvalidOrExpired;

        bool isOtpValid = request.OtpToken is not null &&
                          secureTokenGenerator.VerifyToken(request.OtpToken, recoveryRequest.OtpTokenHash);

        bool isMagicLinkValid = request.MagicLinkToken is not null &&
                                secureTokenGenerator.VerifyToken(request.MagicLinkToken, recoveryRequest.MagicLinkTokenHash);

        if (!isOtpValid && !isMagicLinkValid)
        {
            await attemptTracker.TrackFailedAttemptAsync(request.TokenKey, cancellationToken);
            return RecoveryRequestOperationFaults.InvalidOrExpired;
        }

        Outcome verifyOutcome = recoveryRequest.VerifyNewEmail(dateTimeProvider.UtcNow);

        if (verifyOutcome.IsFailure)
            return verifyOutcome.Fault;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Outcome.Success();
    }
}