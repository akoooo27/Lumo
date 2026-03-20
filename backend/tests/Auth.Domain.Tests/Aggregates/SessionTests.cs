using Auth.Domain.Aggregates;
using Auth.Domain.Constants;
using Auth.Domain.Enums;
using Auth.Domain.Faults;
using Auth.Domain.ValueObjects;

using FluentAssertions;

using SharedKernel;

namespace Auth.Domain.Tests.Aggregates;

public sealed class SessionTests
{
    private static readonly DateTimeOffset UtcNow = DateTimeOffset.UtcNow;
    private static readonly UserId ValidUserId = UserId.New();
    private const string ValidRefreshTokenKey = "refresh-key-123";
    private const string ValidRefreshTokenHash = "hashed-refresh-token";
    private const string ValidSessionIdValue = "sid_01JGX123456789012345678901";

    private static SessionId CreateValidSessionId() => SessionId.UnsafeFrom(ValidSessionIdValue);

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

        Outcome<Session> outcome = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: fingerprint,
            utcNow: UtcNow
        );

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.UserId.Should().Be(ValidUserId);
        outcome.Value.RefreshTokenKey.Should().Be(ValidRefreshTokenKey);
        outcome.Value.RefreshTokenHash.Should().Be(ValidRefreshTokenHash);
        outcome.Value.Fingerprint.Should().Be(fingerprint);
        outcome.Value.CreatedAt.Should().Be(UtcNow);
        outcome.Value.ExpiresAt.Should().Be(UtcNow.AddDays(SessionConstants.SessionExpirationDays));
        outcome.Value.LastRefreshedAt.Should().Be(UtcNow);
        outcome.Value.RevokeReason.Should().BeNull();
        outcome.Value.RevokedAt.Should().BeNull();
        outcome.Value.Version.Should().Be(1);
    }

    [Fact]
    public void Create_WithValidData_ShouldUseProvidedId()
    {
        SessionId expectedId = CreateValidSessionId();

        Outcome<Session> outcome = Session.Create
        (
            id: expectedId,
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
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

        Outcome<Session> outcome = Session.Create
        (
            id: CreateValidSessionId(),
            userId: emptyUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.UserIdRequiredForCreation);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyRefreshTokenKey_ShouldReturnFailure(string? refreshTokenKey)
    {
        Outcome<Session> outcome = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: refreshTokenKey!,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.RefreshTokenKeyRequiredForCreation);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyRefreshTokenHash_ShouldReturnFailure(string? refreshTokenHash)
    {
        Outcome<Session> outcome = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: refreshTokenHash!,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.RefreshTokenHashRequiredForCreation);
    }

    [Fact]
    public void Refresh_WithValidData_ShouldUpdateTokens()
    {
        Fingerprint originalFingerprint = CreateValidFingerprint();
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: originalFingerprint,
            utcNow: UtcNow
        ).Value;

        string newRefreshTokenKey = "new-refresh-key";
        string newRefreshTokenHash = "new-hashed-refresh-token";
        Fingerprint newFingerprint = CreateValidFingerprint();
        DateTimeOffset refreshTime = UtcNow.AddHours(1);

        Outcome outcome = session.Refresh(newRefreshTokenKey, newRefreshTokenHash, newFingerprint, refreshTime);

        outcome.IsSuccess.Should().BeTrue();
        session.RefreshTokenKey.Should().Be(newRefreshTokenKey);
        session.RefreshTokenHash.Should().Be(newRefreshTokenHash);
        session.Fingerprint.Should().Be(newFingerprint);
        session.ExpiresAt.Should().Be(refreshTime.AddDays(SessionConstants.SessionExpirationDays));
        session.LastRefreshedAt.Should().Be(refreshTime);
        session.Version.Should().Be(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Refresh_WithEmptyRefreshTokenKey_ShouldReturnFailure(string? newRefreshTokenKey)
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        Outcome outcome = session.Refresh(newRefreshTokenKey!, "new-hash", CreateValidFingerprint(), UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.RefreshTokenKeyRequiredForRefresh);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Refresh_WithEmptyRefreshTokenHash_ShouldReturnFailure(string? newRefreshTokenHash)
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        Outcome outcome = session.Refresh("new-key", newRefreshTokenHash!, CreateValidFingerprint(), UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.RefreshTokenHashRequiredForRefresh);
    }

    [Fact]
    public void Refresh_WhenExpired_ShouldReturnFailure()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset expiredTime = UtcNow.AddDays(SessionConstants.SessionExpirationDays + 1);

        Outcome outcome = session.Refresh("new-key", "new-hash", CreateValidFingerprint(), expiredTime);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.SessionExpired);
    }

    [Fact]
    public void Refresh_WhenRevoked_ShouldReturnFailure()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        session.RevokeDueToLogout(UtcNow);

        Outcome outcome = session.Refresh("new-key", "new-hash", CreateValidFingerprint(), UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.SessionRevoked);
    }

    [Fact]
    public void RevokeDueToLogout_WhenActive_ShouldRevokeSession()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset revokeTime = UtcNow.AddHours(1);

        Outcome outcome = session.RevokeDueToLogout(revokeTime);

        outcome.IsSuccess.Should().BeTrue();
        session.RevokeReason.Should().Be(SessionRevokeReason.UserLogout);
        session.RevokedAt.Should().Be(revokeTime);
    }

    [Fact]
    public void RevokeDueToLogout_WhenExpired_ShouldReturnFailure()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset expiredTime = UtcNow.AddDays(SessionConstants.SessionExpirationDays + 1);

        Outcome outcome = session.RevokeDueToLogout(expiredTime);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.SessionExpired);
    }

    [Fact]
    public void RevokeDueToLogout_WhenAlreadyRevoked_ShouldReturnFailure()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        session.RevokeDueToLogout(UtcNow);

        Outcome outcome = session.RevokeDueToLogout(UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.SessionRevoked);
    }

    [Fact]
    public void RevokeDueToEmailChange_WhenActive_ShouldRevokeSession()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset revokeTime = UtcNow.AddHours(1);

        Outcome outcome = session.RevokeDueToEmailChange(revokeTime);

        outcome.IsSuccess.Should().BeTrue();
        session.RevokeReason.Should().Be(SessionRevokeReason.EmailChange);
        session.RevokedAt.Should().Be(revokeTime);
    }

    [Fact]
    public void RevokeDueToEmailChange_WhenExpired_ShouldReturnFailure()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset expiredTime = UtcNow.AddDays(SessionConstants.SessionExpirationDays + 1);

        Outcome outcome = session.RevokeDueToEmailChange(expiredTime);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.SessionExpired);
    }

    [Fact]
    public void RevokeDueToEmailChange_WhenAlreadyRevoked_ShouldReturnFailure()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        session.RevokeDueToLogout(UtcNow);

        Outcome outcome = session.RevokeDueToEmailChange(UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.SessionRevoked);
    }

    [Fact]
    public void RevokeDueToAccountRecovery_WhenActive_ShouldRevokeSession()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset revokeTime = UtcNow.AddHours(1);

        Outcome outcome = session.RevokeDueToAccountRecovery(revokeTime);

        outcome.IsSuccess.Should().BeTrue();
        session.RevokeReason.Should().Be(SessionRevokeReason.AccountRecovery);
        session.RevokedAt.Should().Be(revokeTime);
    }

    [Fact]
    public void RevokeDueToAccountRecovery_WhenExpired_ShouldReturnFailure()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset expiredTime = UtcNow.AddDays(SessionConstants.SessionExpirationDays + 1);

        Outcome outcome = session.RevokeDueToAccountRecovery(expiredTime);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.SessionExpired);
    }

    [Fact]
    public void RevokeDueToAccountRecovery_WhenAlreadyRevoked_ShouldReturnFailure()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        session.RevokeDueToLogout(UtcNow);

        Outcome outcome = session.RevokeDueToAccountRecovery(UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.SessionRevoked);
    }

    [Fact]
    public void RevokeDueToOldRefreshTokenUsage_WhenActive_ShouldRevokeSession()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset revokeTime = UtcNow.AddHours(1);

        Outcome outcome = session.RevokeDueToOldRefreshTokenUsage(revokeTime);

        outcome.IsSuccess.Should().BeTrue();
        session.RevokeReason.Should().Be(SessionRevokeReason.OldRefreshTokenUsed);
        session.RevokedAt.Should().Be(revokeTime);
    }

    [Fact]
    public void RevokeDueToOldRefreshTokenUsage_WhenExpired_ShouldReturnFailure()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        DateTimeOffset expiredTime = UtcNow.AddDays(SessionConstants.SessionExpirationDays + 1);

        Outcome outcome = session.RevokeDueToOldRefreshTokenUsage(expiredTime);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.SessionExpired);
    }

    [Fact]
    public void RevokeDueToOldRefreshTokenUsage_WhenAlreadyRevoked_ShouldReturnFailure()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        session.RevokeDueToLogout(UtcNow);

        Outcome outcome = session.RevokeDueToOldRefreshTokenUsage(UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SessionFaults.SessionRevoked);
    }

    [Fact]
    public void Refresh_WhenValid_ShouldStoreOldRefreshToken()
    {
        Session session = Session.Create
        (
            id: CreateValidSessionId(),
            userId: ValidUserId,
            refreshTokenKey: ValidRefreshTokenKey,
            refreshTokenHash: ValidRefreshTokenHash,
            fingerprint: CreateValidFingerprint(),
            utcNow: UtcNow
        ).Value;

        session.OldRefreshTokenKey.Should().BeNull();
        session.OldRefreshTokenHash.Should().BeNull();

        DateTimeOffset refreshTime = UtcNow.AddHours(1);

        session.Refresh("new-key", "new-hash", CreateValidFingerprint(), refreshTime);

        session.OldRefreshTokenKey.Should().Be(ValidRefreshTokenKey);
        session.OldRefreshTokenHash.Should().Be(ValidRefreshTokenHash);
        session.RefreshTokenKey.Should().Be("new-key");
        session.RefreshTokenHash.Should().Be("new-hash");
    }
}