using Auth.Domain.Aggregates;
using Auth.Domain.Constants;
using Auth.Domain.Faults;
using Auth.Domain.ValueObjects;

using FluentAssertions;

using SharedKernel;

namespace Auth.Domain.Tests.Aggregates;

public sealed class RecoveryRequestTests
{
    private static readonly DateTimeOffset UtcNow = DateTimeOffset.UtcNow;
    private static readonly UserId ValidUserId = UserId.New();
    private static readonly EmailAddress ValidNewEmail = EmailAddress.UnsafeFromString("new@example.com");
    private const string ValidTokenKey = "token-key-123";
    private const string ValidOtpTokenHash = "hashed-otp-token";
    private const string ValidMagicLinkTokenHash = "hashed-magic-link-token";
    private const string ValidRecoveryRequestIdValue = "rr_01JGX123456789012345678901";

    private static RecoveryRequestId CreateValidRecoveryRequestId() => RecoveryRequestId.UnsafeFrom(ValidRecoveryRequestIdValue);

    private static Fingerprint CreateValidFingerprint()
    {
        return Fingerprint.Create
        (
            ipAddress: "192.168.1.1",
            userAgent: "Mozilla/5.0",
            timezone: "Europe/London",
            language: "en-US",
            normalizedBrowser: "Chrome 120",
            normalizedOs: "Windows 11"
        ).Value;
    }

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        Fingerprint fingerprint = CreateValidFingerprint();

        Outcome<RecoveryRequest> outcome = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: fingerprint,
            utcNow: UtcNow
        );

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.UserId.Should().Be(ValidUserId);
        outcome.Value.TokenKey.Should().Be(ValidTokenKey);
        outcome.Value.NewEmailAddress.Should().Be(ValidNewEmail);
        outcome.Value.OtpTokenHash.Should().Be(ValidOtpTokenHash);
        outcome.Value.MagicLinkTokenHash.Should().Be(ValidMagicLinkTokenHash);
        outcome.Value.Fingerprint.Should().Be(fingerprint);
        outcome.Value.CreatedAt.Should().Be(UtcNow);
        outcome.Value.ExpiresAt.Should().Be(UtcNow.AddMinutes(RecoveryRequestConstants.ExpirationMinutes));
        outcome.Value.NewEmailVerifiedAt.Should().BeNull();
        outcome.Value.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithValidData_ShouldUseProvidedId()
    {
        RecoveryRequestId expectedId = CreateValidRecoveryRequestId();

        Outcome<RecoveryRequest> outcome = RecoveryRequest.Create
        (
            id: expectedId,
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        );

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Id.Should().Be(expectedId);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldReturnFailure()
    {
        UserId emptyUserId = UserId.UnsafeFromGuid(Guid.Empty);

        Outcome<RecoveryRequest> outcome = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: emptyUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryRequestFaults.UserIdRequiredForCreation);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyTokenKey_ShouldReturnFailure(string? tokenKey)
    {
        Outcome<RecoveryRequest> outcome = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: tokenKey!,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryRequestFaults.TokenKeyRequiredForCreation);
    }

    [Fact]
    public void Create_WithEmptyNewEmail_ShouldReturnFailure()
    {
        EmailAddress emptyEmail = EmailAddress.UnsafeFromString(string.Empty);

        Outcome<RecoveryRequest> outcome = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: emptyEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryRequestFaults.NewEmailRequiredForCreation);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyOtpTokenHash_ShouldReturnFailure(string? otpTokenHash)
    {
        Outcome<RecoveryRequest> outcome = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: otpTokenHash!,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryRequestFaults.OtpTokenHashRequiredForCreation);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyMagicLinkTokenHash_ShouldReturnFailure(string? magicLinkTokenHash)
    {
        Outcome<RecoveryRequest> outcome = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: magicLinkTokenHash!,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryRequestFaults.MagicLinkTokenHashRequiredForCreation);
    }

    [Fact]
    public void VerifyNewEmail_WhenValid_ShouldSetNewEmailVerifiedAt()
    {
        RecoveryRequest request = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset verifyTime = UtcNow.AddMinutes(5);

        Outcome outcome = request.VerifyNewEmail(verifyTime);

        outcome.IsSuccess.Should().BeTrue();
        request.NewEmailVerifiedAt.Should().Be(verifyTime);
        request.IsNewEmailVerified.Should().BeTrue();
    }

    [Fact]
    public void VerifyNewEmail_WhenAlreadyCompleted_ShouldReturnFailure()
    {
        RecoveryRequest request = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        request.VerifyNewEmail(UtcNow);
        request.Complete(UtcNow);

        Outcome outcome = request.VerifyNewEmail(UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryRequestFaults.AlreadyCompleted);
    }

    [Fact]
    public void VerifyNewEmail_WhenExpired_ShouldReturnFailure()
    {
        RecoveryRequest request = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset expiredTime = UtcNow.AddMinutes(RecoveryRequestConstants.ExpirationMinutes + 1);

        Outcome outcome = request.VerifyNewEmail(expiredTime);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryRequestFaults.Expired);
    }

    [Fact]
    public void VerifyNewEmail_WhenAlreadyVerified_ShouldReturnFailure()
    {
        RecoveryRequest request = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        request.VerifyNewEmail(UtcNow);

        Outcome outcome = request.VerifyNewEmail(UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryRequestFaults.NewEmailAlreadyVerified);
    }

    [Fact]
    public void Complete_WhenValid_ShouldSetCompletedAt()
    {
        RecoveryRequest request = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        request.VerifyNewEmail(UtcNow);

        DateTimeOffset completeTime = UtcNow.AddMinutes(5);

        Outcome outcome = request.Complete(completeTime);

        outcome.IsSuccess.Should().BeTrue();
        request.CompletedAt.Should().Be(completeTime);
        request.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldReturnFailure()
    {
        RecoveryRequest request = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        request.VerifyNewEmail(UtcNow);
        request.Complete(UtcNow);

        Outcome outcome = request.Complete(UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryRequestFaults.AlreadyCompleted);
    }

    [Fact]
    public void Complete_WhenExpired_ShouldReturnFailure()
    {
        RecoveryRequest request = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        request.VerifyNewEmail(UtcNow);

        DateTimeOffset expiredTime = UtcNow.AddMinutes(RecoveryRequestConstants.ExpirationMinutes + 1);

        Outcome outcome = request.Complete(expiredTime);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryRequestFaults.Expired);
    }

    [Fact]
    public void Complete_WhenNewEmailNotVerified_ShouldReturnFailure()
    {
        RecoveryRequest request = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        Outcome outcome = request.Complete(UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryRequestFaults.NewEmailNotVerified);
    }

    [Fact]
    public void Complete_JustBeforeExpiration_ShouldSucceed()
    {
        RecoveryRequest request = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        request.VerifyNewEmail(UtcNow);

        DateTimeOffset justBeforeExpiration = UtcNow.AddMinutes(RecoveryRequestConstants.ExpirationMinutes).AddSeconds(-1);

        Outcome outcome = request.Complete(justBeforeExpiration);

        outcome.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenNotExpired_ShouldReturnFalse()
    {
        RecoveryRequest request = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        bool isExpired = request.IsExpired(UtcNow);

        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpired_ShouldReturnTrue()
    {
        RecoveryRequest request = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset expiredTime = UtcNow.AddMinutes(RecoveryRequestConstants.ExpirationMinutes + 1);

        bool isExpired = request.IsExpired(expiredTime);

        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_AtExactExpirationTime_ShouldReturnTrue()
    {
        RecoveryRequest request = RecoveryRequest.Create
        (
            id: CreateValidRecoveryRequestId(),
            userId: ValidUserId,
            tokenKey: ValidTokenKey,
            newEmailAddress: ValidNewEmail,
            otpTokenHash: ValidOtpTokenHash,
            magicLinkTokenHash: ValidMagicLinkTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset exactExpirationTime = UtcNow.AddMinutes(RecoveryRequestConstants.ExpirationMinutes);

        bool isExpired = request.IsExpired(exactExpirationTime);

        // At exact expiration time, the request is expired (ExpiresAt <= utcNow)
        // This is consistent with LoginRequest, Session, and EmailChangeRequest
        isExpired.Should().BeTrue();
    }
}