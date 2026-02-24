using Auth.Application.Abstractions.Authentication;
using Auth.Application.Abstractions.Data;
using Auth.Domain.Aggregates;
using Auth.Domain.Constants;
using Auth.Domain.Faults;
using Auth.Domain.ValueObjects;

using Contracts.IntegrationEvents.Auth;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.Sessions.RefreshToken;

internal sealed class RefreshTokenHandler(
    IAuthDbContext dbContext,
    IRequestContext requestContext,
    ISecureTokenGenerator secureTokenGenerator,
    ITokenProvider tokenProvider,
    IMessageBus messageBus,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async ValueTask<Outcome<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        Outcome<Fingerprint> fingerprintOutcome = Fingerprint.Create
        (
            ipAddress: requestContext.IpAddress,
            userAgent: requestContext.UserAgent,
            timezone: requestContext.Timezone,
            language: requestContext.Language,
            normalizedBrowser: requestContext.NormalizedBrowser,
            normalizedOs: requestContext.NormalizedOs
        );

        if (fingerprintOutcome.IsFailure)
            return fingerprintOutcome.Fault;

        Fingerprint fingerprint = fingerprintOutcome.Value;

        if (!secureTokenGenerator.TryParseCompoundToken(request.RefreshToken, out string refreshTokenKey, out string refreshToken))
            return SessionFaults.RefreshTokenInvalidOrExpired;

        Session? session = await dbContext.Sessions
            .FirstOrDefaultAsync(s => s.RefreshTokenKey == refreshTokenKey || s.OldRefreshTokenKey == refreshTokenKey,
                cancellationToken);

        if (session is null)
            return SessionFaults.RefreshTokenInvalidOrExpired;

        if (session.OldRefreshTokenKey == refreshTokenKey)
        {
            if (session.OldRefreshTokenHash is not null)
            {
                bool isSameAsOldToken = secureTokenGenerator.VerifyToken(refreshToken, session.OldRefreshTokenHash);

                if (isSameAsOldToken)
                {
                    session.RevokeDueToOldRefreshTokenUsage(dateTimeProvider.UtcNow);

                    User? user = await dbContext.Users
                        .FirstOrDefaultAsync(u => u.Id == session.UserId, cancellationToken);

                    if (user is not null)
                    {
                        OldRefreshTokenUsed oldRefreshTokenUsed = new()
                        {
                            EventId = Guid.NewGuid(),
                            OccurredAt = dateTimeProvider.UtcNow,
                            CorrelationId = Guid.Parse(requestContext.CorrelationId),
                            UserId = session.UserId.Value,
                            EmailAddress = user.EmailAddress.Value,
                            IpAddress = requestContext.IpAddress,
                            UserAgent = requestContext.UserAgent
                        };

                        await messageBus.PublishAsync(oldRefreshTokenUsed, cancellationToken);
                    }

                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            return SessionFaults.RefreshTokenInvalidOrExpired;
        }

        bool isValid = secureTokenGenerator.VerifyToken(refreshToken, session.RefreshTokenHash);

        if (!isValid)
            return SessionFaults.RefreshTokenInvalidOrExpired;

        string newRefreshTokenKey = secureTokenGenerator.GenerateToken(SessionConstants.RefreshTokenKeyLength);
        string newRefreshToken = secureTokenGenerator.GenerateToken(SessionConstants.RefreshTokenLength);
        string newRefreshTokenHash = secureTokenGenerator.HashToken(newRefreshToken);

        Outcome refreshOutcome = session.Refresh
        (
            newRefreshTokenKey: newRefreshTokenKey,
            newRefreshTokenHash: newRefreshTokenHash,
            fingerprint: fingerprint,
            dateTimeProvider.UtcNow
        );

        if (refreshOutcome.IsFailure)
            return SessionFaults.RefreshTokenInvalidOrExpired;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return SessionFaults.RefreshTokenInvalidOrExpired;
        }

        string accessToken = tokenProvider.CreateToken(session.UserId.Value, session.Id.Value);

        RefreshTokenResponse refreshTokenResponse = new
        (
            AccessToken: accessToken,
            RefreshToken: $"{newRefreshTokenKey}.{newRefreshToken}"
        );

        return refreshTokenResponse;
    }
}