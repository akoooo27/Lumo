using System.Diagnostics.CodeAnalysis;

using Auth.Domain.Constants;
using Auth.Domain.Faults;
using Auth.Domain.ValueObjects;

using SharedKernel;

namespace Auth.Domain.Aggregates;

public sealed class RecoveryRequest : AggregateRoot<RecoveryRequestId>
{
    public UserId UserId { get; private set; }

    public string TokenKey { get; private set; } = string.Empty;

    public EmailAddress NewEmailAddress { get; private set; }

    public string OtpTokenHash { get; private set; } = string.Empty;

    public string MagicLinkTokenHash { get; private set; } = string.Empty;

    public Fingerprint Fingerprint { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? NewEmailVerifiedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    private RecoveryRequest() { } // For EF Core

    [SetsRequiredMembers]
    private RecoveryRequest
    (
        RecoveryRequestId id,
        UserId userId,
        string tokenKey,
        EmailAddress newEmailAddress,
        string otpTokenHash,
        string magicLinkTokenHash,
        Fingerprint fingerprint,
        DateTimeOffset utcNow
    )
    {
        Id = id;
        UserId = userId;
        TokenKey = tokenKey;
        NewEmailAddress = newEmailAddress;
        OtpTokenHash = otpTokenHash;
        MagicLinkTokenHash = magicLinkTokenHash;
        Fingerprint = fingerprint;
        CreatedAt = utcNow;
        ExpiresAt = utcNow.AddMinutes(RecoveryRequestConstants.ExpirationMinutes);
        NewEmailVerifiedAt = null;
        CompletedAt = null;
    }

    public static Outcome<RecoveryRequest> Create
    (
        RecoveryRequestId id,
        UserId userId,
        string tokenKey,
        EmailAddress newEmailAddress,
        string otpTokenHash,
        string magicLinkTokenHash,
        Fingerprint fingerprint,
        DateTimeOffset utcNow
    )
    {
        if (userId.IsEmpty)
            return RecoveryRequestFaults.UserIdRequiredForCreation;

        if (string.IsNullOrWhiteSpace(tokenKey))
            return RecoveryRequestFaults.TokenKeyRequiredForCreation;

        if (newEmailAddress.IsEmpty())
            return RecoveryRequestFaults.NewEmailRequiredForCreation;

        if (string.IsNullOrWhiteSpace(otpTokenHash))
            return RecoveryRequestFaults.OtpTokenHashRequiredForCreation;

        if (string.IsNullOrWhiteSpace(magicLinkTokenHash))
            return RecoveryRequestFaults.MagicLinkTokenHashRequiredForCreation;

        RecoveryRequest recoveryRequest = new
        (
            id: id,
            userId: userId,
            tokenKey: tokenKey,
            newEmailAddress: newEmailAddress,
            otpTokenHash: otpTokenHash,
            magicLinkTokenHash: magicLinkTokenHash,
            fingerprint: fingerprint,
            utcNow: utcNow
        );

        return recoveryRequest;
    }

    public Outcome VerifyNewEmail(DateTimeOffset utcNow)
    {
        if (CompletedAt is not null)
            return RecoveryRequestFaults.AlreadyCompleted;

        if (ExpiresAt <= utcNow)
            return RecoveryRequestFaults.Expired;

        if (NewEmailVerifiedAt is not null)
            return RecoveryRequestFaults.NewEmailAlreadyVerified;

        NewEmailVerifiedAt = utcNow;

        return Outcome.Success();
    }

    public Outcome Complete(DateTimeOffset utcNow)
    {
        if (CompletedAt is not null)
            return RecoveryRequestFaults.AlreadyCompleted;

        if (ExpiresAt <= utcNow)
            return RecoveryRequestFaults.Expired;

        if (NewEmailVerifiedAt is null)
            return RecoveryRequestFaults.NewEmailNotVerified;

        CompletedAt = utcNow;

        return Outcome.Success();
    }

    public bool IsNewEmailVerified => NewEmailVerifiedAt is not null;

    public bool IsCompleted => CompletedAt is not null;

    public bool IsExpired(DateTimeOffset utcNow) => ExpiresAt <= utcNow;
}