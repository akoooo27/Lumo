using Auth.Application.Abstractions.Authentication;
using Auth.Application.Abstractions.Data;
using Auth.Application.Abstractions.Generators;
using Auth.Domain.Aggregates;
using Auth.Domain.Constants;
using Auth.Domain.ValueObjects;

using Contracts.IntegrationEvents.Auth;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.LoginRequests.Create;

internal sealed class CreateLoginHandler(
    IAuthDbContext dbContext,
    ISecureTokenGenerator secureTokenGenerator,
    IRequestContext requestContext,
    IIdGenerator idGenerator,
    IAttemptTracker attemptTracker,
    IMessageBus messageBus,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateLoginCommand, CreateLoginResponse>
{
    public async ValueTask<Outcome<CreateLoginResponse>> Handle(CreateLoginCommand request,
        CancellationToken cancellationToken)
    {
        Outcome<EmailAddress> emailAddressOutcome = EmailAddress.Create(request.EmailAddress);

        if (emailAddressOutcome.IsFailure)
            return emailAddressOutcome.Fault;

        EmailAddress emailAddress = emailAddressOutcome.Value;

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

        (string tokenKey, string otpToken, string magicLinkToken) = GenerateTokens();

#pragma warning disable CA1308
        string cooldownKey = emailAddress.Value.ToLowerInvariant();
#pragma warning restore CA1308
        bool isOnCooldown = await attemptTracker.IsCooldownActiveAsync(cooldownKey, cancellationToken);

        if (isOnCooldown)
            return new CreateLoginResponse
            (
                TokenKey: tokenKey,
                ExpiresAt: dateTimeProvider.UtcNow.AddMinutes(LoginRequestConstants.ExpirationMinutes)
            );

        string otpTokenHash = secureTokenGenerator.HashToken(otpToken);
        string magicLinkTokenHash = secureTokenGenerator.HashToken(magicLinkToken);

        User? user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.EmailAddress == emailAddress, cancellationToken);

        if (user is null)
            return new CreateLoginResponse
            (
                TokenKey: tokenKey,
                ExpiresAt: dateTimeProvider.UtcNow.AddMinutes(LoginRequestConstants.ExpirationMinutes)
            );

        LoginRequestId id = idGenerator.NewLoginRequestId();

        Outcome<LoginRequest> loginRequestOutcome = LoginRequest.Create
        (
            id: id,
            userId: user.Id,
            tokenKey: tokenKey,
            otpTokenHash: otpTokenHash,
            magicLinkTokenHash: magicLinkTokenHash,
            fingerprint: fingerprint,
            utcNow: dateTimeProvider.UtcNow
        );

        if (loginRequestOutcome.IsFailure)
            return loginRequestOutcome.Fault;

        LoginRequest loginRequest = loginRequestOutcome.Value;

        LoginRequested loginRequested = new()
        {
            EventId = Guid.NewGuid(),
            OccurredAt = dateTimeProvider.UtcNow,
            CorrelationId = Guid.Parse(requestContext.CorrelationId),
            EmailAddress = emailAddress.Value,
            OtpToken = otpToken,
            MagicLinkToken = magicLinkToken,
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            ExpiresAt = loginRequest.ExpiresAt
        };

        await dbContext.LoginRequests.AddAsync(loginRequest, cancellationToken);
        await messageBus.PublishAsync(loginRequested, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await attemptTracker.SetCooldownAsync(cooldownKey, cancellationToken);

        return new CreateLoginResponse
        (
            TokenKey: tokenKey,
            ExpiresAt: loginRequest.ExpiresAt
        );
    }

    private (string tokenKey, string otpToken, string magicLinkToken) GenerateTokens()
    {
        string tokenKey = secureTokenGenerator.GenerateToken(LoginRequestConstants.TokenKeyLength);
        string otpToken = secureTokenGenerator.GenerateToken(LoginRequestConstants.OtpTokenLength);
        string magicLinkToken = secureTokenGenerator.GenerateToken(LoginRequestConstants.MagicLinkTokenLength);
        return (tokenKey, otpToken, magicLinkToken);
    }
}