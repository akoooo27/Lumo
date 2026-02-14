using Auth.Application.Abstractions.Authentication;
using Auth.Application.Abstractions.Data;
using Auth.Application.Abstractions.Generators;
using Auth.Application.Faults;
using Auth.Domain.Aggregates;
using Auth.Domain.Constants;
using Auth.Domain.Faults;
using Auth.Domain.ValueObjects;

using Contracts.IntegrationEvents.Auth;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.LoginRequests.Verify;

internal sealed class VerifyLoginHandler(
    IAuthDbContext dbContext,
    IRequestContext requestContext,
    ISecureTokenGenerator secureTokenGenerator,
    IAttemptTracker attemptTracker,
    IIdGenerator idGenerator,
    ITokenProvider tokenProvider,
    IMessageBus messageBus,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<VerifyLoginCommand, VerifyLoginResponse>
{
    public async ValueTask<Outcome<VerifyLoginResponse>> Handle(VerifyLoginCommand request,
        CancellationToken cancellationToken)
    {
        bool isLocked = await attemptTracker.IsLockedAsync(request.TokenKey, cancellationToken);

        if (isLocked)
            return LoginRequestOperationFaults.TooManyAttempts;

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

        var data = await
        (
            from lr in dbContext.LoginRequests
            join u in dbContext.Users on lr.UserId equals u.Id
            where lr.TokenKey == request.TokenKey
            select new { LoginRequest = lr, u.EmailAddress }
        ).FirstOrDefaultAsync(cancellationToken);

        if (data is null)
            return LoginRequestFaults.InvalidOrExpired;

        LoginRequest loginRequest = data.LoginRequest;
        EmailAddress emailAddress = data.EmailAddress;

        bool isOtpValid = request.OtpToken is not null &&
                          secureTokenGenerator.VerifyToken(request.OtpToken, loginRequest.OtpTokenHash);

        bool isMagicLinkValid = request.MagicLinkToken is not null &&
                                secureTokenGenerator.VerifyToken(request.MagicLinkToken,
                                    loginRequest.MagicLinkTokenHash);

        bool isValid = isOtpValid || isMagicLinkValid;

        if (!isValid)
        {
            await attemptTracker.TrackFailedAttemptAsync(request.TokenKey, cancellationToken);
            return LoginRequestFaults.InvalidOrExpired;
        }

        Outcome consumeOutcome = loginRequest.Consume(dateTimeProvider.UtcNow);

        if (consumeOutcome.IsFailure)
            return consumeOutcome.Fault;

        string refreshTokenKey = secureTokenGenerator.GenerateToken(SessionConstants.RefreshTokenKeyLength);
        string refreshToken = secureTokenGenerator.GenerateToken(SessionConstants.RefreshTokenLength);
        string refreshTokenHash = secureTokenGenerator.HashToken(refreshToken);

        SessionId sessionId = idGenerator.NewSessionId();

        Outcome<Session> sessionOutcome = Session.Create
        (
            id: sessionId,
            userId: loginRequest.UserId,
            refreshTokenKey: refreshTokenKey,
            refreshTokenHash: refreshTokenHash,
            fingerprint: fingerprint,
            utcNow: dateTimeProvider.UtcNow
        );

        if (sessionOutcome.IsFailure)
            return sessionOutcome.Fault;

        Session session = sessionOutcome.Value;

        LoginVerified loginVerified = new()
        {
            EventId = Guid.NewGuid(),
            OccurredAt = dateTimeProvider.UtcNow,
            CorrelationId = Guid.Parse(requestContext.CorrelationId),
            UserId = loginRequest.UserId.Value,
            EmailAddress = emailAddress.Value,
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent
        };

        await dbContext.Sessions.AddAsync(session, cancellationToken);
        await messageBus.PublishAsync(loginVerified, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        string accessToken = tokenProvider.CreateToken(loginRequest.UserId.Value, session.Id.Value);

        VerifyLoginResponse verifyLoginResponse = new
        (
            AccessToken: accessToken,
            RefreshToken: $"{refreshTokenKey}.{refreshToken}"
        );

        return verifyLoginResponse;
    }
}