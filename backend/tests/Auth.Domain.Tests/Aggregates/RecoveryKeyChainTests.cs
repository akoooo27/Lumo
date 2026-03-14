using Auth.Domain.Aggregates;
using Auth.Domain.Constants;
using Auth.Domain.Faults;
using Auth.Domain.ValueObjects;

using FluentAssertions;

using SharedKernel;

namespace Auth.Domain.Tests.Aggregates;

public sealed class RecoveryKeyChainTests
{
    private static readonly DateTimeOffset UtcNow = DateTimeOffset.UtcNow;
    private static readonly UserId ValidUserId = UserId.New();
    private const string ValidRecoveryKeyChainIdValue = "rkc_01JGX123456789012345678901";

    private static RecoveryKeyChainId CreateValidRecoveryKeyChainId() => RecoveryKeyChainId.UnsafeFrom(ValidRecoveryKeyChainIdValue);

    private static List<RecoverKeyInput> CreateValidRecoveryKeyInputs()
    {
        return Enumerable.Range(1, RecoveryKeyConstants.MaxKeysPerChain)
            .Select(i => RecoverKeyInput.Create($"identifier-{i}", $"verifier-hash-{i}"))
            .ToList();
    }

    private static List<(string identifier, string verifierHash)> CreateValidRecoveryKeyPairs()
    {
        return Enumerable.Range(1, RecoveryKeyConstants.MaxKeysPerChain)
            .Select(i => ($"new-identifier-{i}", $"new-verifier-hash-{i}"))
            .ToList();
    }

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        List<RecoverKeyInput> inputs = CreateValidRecoveryKeyInputs();

        Outcome<RecoveryKeyChain> outcome = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: inputs,
            utcNow: UtcNow
        );

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.UserId.Should().Be(ValidUserId);
        outcome.Value.CreatedAt.Should().Be(UtcNow);
        outcome.Value.LastRotatedAt.Should().BeNull();
        outcome.Value.Version.Should().Be(1);
        outcome.Value.RecoveryKeys.Should().HaveCount(RecoveryKeyConstants.MaxKeysPerChain);
    }

    [Fact]
    public void Create_WithValidData_ShouldUseProvidedId()
    {
        List<RecoverKeyInput> inputs = CreateValidRecoveryKeyInputs();
        RecoveryKeyChainId expectedId = CreateValidRecoveryKeyChainId();

        Outcome<RecoveryKeyChain> outcome = RecoveryKeyChain.Create
        (
            id: expectedId,
            userId: ValidUserId,
            recoverKeyInputs: inputs,
            utcNow: UtcNow
        );

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Id.Should().Be(expectedId);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldReturnFailure()
    {
        UserId emptyUserId = UserId.UnsafeFromGuid(Guid.Empty);
        List<RecoverKeyInput> inputs = CreateValidRecoveryKeyInputs();

        Outcome<RecoveryKeyChain> outcome = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: emptyUserId,
            recoverKeyInputs: inputs,
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryKeyChainFaults.UserIdRequiredForCreation);
    }

    [Fact]
    public void Create_WithTooFewKeys_ShouldReturnFailure()
    {
        List<RecoverKeyInput> inputs = Enumerable.Range(1, RecoveryKeyConstants.MaxKeysPerChain - 1)
            .Select(i => RecoverKeyInput.Create($"identifier-{i}", $"verifier-hash-{i}"))
            .ToList();

        Outcome<RecoveryKeyChain> outcome = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: inputs,
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryKeyChainFaults.InvalidRecoveryKeyCount);
    }

    [Fact]
    public void Create_WithTooManyKeys_ShouldReturnFailure()
    {
        List<RecoverKeyInput> inputs = Enumerable.Range(1, RecoveryKeyConstants.MaxKeysPerChain + 1)
            .Select(i => RecoverKeyInput.Create($"identifier-{i}", $"verifier-hash-{i}"))
            .ToList();

        Outcome<RecoveryKeyChain> outcome = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: inputs,
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryKeyChainFaults.InvalidRecoveryKeyCount);
    }

    [Fact]
    public void Create_WithEmptyKeys_ShouldReturnFailure()
    {
        List<RecoverKeyInput> inputs = [];

        Outcome<RecoveryKeyChain> outcome = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: inputs,
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryKeyChainFaults.InvalidRecoveryKeyCount);
    }

    [Fact]
    public void Create_WithNullKeys_ShouldReturnFailure()
    {
        Outcome<RecoveryKeyChain> outcome = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: null!,
            utcNow: UtcNow
        );

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryKeyChainFaults.InvalidRecoveryKeyCount);
    }

    [Fact]
    public void Rotate_WithValidData_ShouldReplaceKeys()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        List<(string identifier, string verifierHash)> newPairs = CreateValidRecoveryKeyPairs();
        DateTimeOffset rotateTime = UtcNow.AddDays(1);

        Outcome outcome = chain.Rotate(newPairs, rotateTime);

        outcome.IsSuccess.Should().BeTrue();
        chain.RecoveryKeys.Should().HaveCount(RecoveryKeyConstants.MaxKeysPerChain);
        chain.LastRotatedAt.Should().Be(rotateTime);
        chain.Version.Should().Be(2);
    }

    [Fact]
    public void Rotate_WithValidData_ShouldUpdateRecoveryKeys()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        List<(string identifier, string verifierHash)> newPairs = CreateValidRecoveryKeyPairs();

        chain.Rotate(newPairs, UtcNow.AddDays(1));

        chain.RecoveryKeys.All(k => k.Identifier.StartsWith("new-", StringComparison.Ordinal)).Should().BeTrue();
    }

    [Fact]
    public void Rotate_WithTooFewKeys_ShouldReturnFailure()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        List<(string identifier, string verifierHash)> newPairs = Enumerable.Range(1, RecoveryKeyConstants.MaxKeysPerChain - 1)
            .Select(i => ($"new-identifier-{i}", $"new-verifier-hash-{i}"))
            .ToList();

        Outcome outcome = chain.Rotate(newPairs, UtcNow.AddDays(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryKeyChainFaults.InvalidRecoveryKeyCount);
    }

    [Fact]
    public void Rotate_WithTooManyKeys_ShouldReturnFailure()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        List<(string identifier, string verifierHash)> newPairs = Enumerable.Range(1, RecoveryKeyConstants.MaxKeysPerChain + 1)
            .Select(i => ($"new-identifier-{i}", $"new-verifier-hash-{i}"))
            .ToList();

        Outcome outcome = chain.Rotate(newPairs, UtcNow.AddDays(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryKeyChainFaults.InvalidRecoveryKeyCount);
    }

    [Fact]
    public void Rotate_WithEmptyKeys_ShouldReturnFailure()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        List<(string identifier, string verifierHash)> newPairs = [];

        Outcome outcome = chain.Rotate(newPairs, UtcNow.AddDays(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryKeyChainFaults.InvalidRecoveryKeyCount);
    }

    [Fact]
    public void Rotate_WithNullKeys_ShouldReturnFailure()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        Outcome outcome = chain.Rotate(null!, UtcNow.AddDays(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryKeyChainFaults.InvalidRecoveryKeyCount);
    }

    [Fact]
    public void Rotate_MultipleTimes_ShouldIncrementVersion()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        chain.Rotate(CreateValidRecoveryKeyPairs(), UtcNow.AddDays(1));
        chain.Rotate(CreateValidRecoveryKeyPairs(), UtcNow.AddDays(2));
        chain.Rotate(CreateValidRecoveryKeyPairs(), UtcNow.AddDays(3));

        chain.Version.Should().Be(4);
    }

    [Fact]
    public void MarkKeyAsUsed_WithValidKey_ShouldMarkKeyAsUsed()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        Fingerprint fingerprint = CreateValidFingerprint();
        DateTimeOffset useTime = UtcNow.AddHours(1);

        Outcome outcome = chain.MarkKeyAsUsed("identifier-1", fingerprint, useTime);

        outcome.IsSuccess.Should().BeTrue();
        chain.RecoveryKeys.First(k => k.Identifier == "identifier-1").IsUsed.Should().BeTrue();
    }

    [Fact]
    public void MarkKeyAsUsed_WithNonExistentKey_ShouldReturnFailure()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        Fingerprint fingerprint = CreateValidFingerprint();

        Outcome outcome = chain.MarkKeyAsUsed("non-existent", fingerprint, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryKeyChainFaults.KeyNotFoundOrUsed);
    }

    [Fact]
    public void MarkKeyAsUsed_WithAlreadyUsedKey_ShouldReturnFailure()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        Fingerprint fingerprint = CreateValidFingerprint();
        chain.MarkKeyAsUsed("identifier-1", fingerprint, UtcNow);

        Outcome outcome = chain.MarkKeyAsUsed("identifier-1", fingerprint, UtcNow.AddHours(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(RecoveryKeyChainFaults.KeyNotFoundOrUsed);
    }

    [Fact]
    public void GetVerifierHashForKey_WithExistingUnusedKey_ShouldReturnHash()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        string? hash = chain.GetVerifierHashForKey("identifier-1");

        hash.Should().Be("verifier-hash-1");
    }

    [Fact]
    public void GetVerifierHashForKey_WithNonExistentKey_ShouldReturnNull()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        string? hash = chain.GetVerifierHashForKey("non-existent");

        hash.Should().BeNull();
    }

    [Fact]
    public void GetVerifierHashForKey_WithUsedKey_ShouldReturnNull()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        Fingerprint fingerprint = CreateValidFingerprint();
        chain.MarkKeyAsUsed("identifier-1", fingerprint, UtcNow);

        string? hash = chain.GetVerifierHashForKey("identifier-1");

        hash.Should().BeNull();
    }

    [Fact]
    public void GetRemainingKeyCount_WithAllUnused_ShouldReturnTotalCount()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        int count = chain.GetRemainingKeyCount();

        count.Should().Be(RecoveryKeyConstants.MaxKeysPerChain);
    }

    [Fact]
    public void GetRemainingKeyCount_AfterUsingKeys_ShouldReturnDecrementedCount()
    {
        RecoveryKeyChain chain = RecoveryKeyChain.Create
        (
            id: CreateValidRecoveryKeyChainId(),
            userId: ValidUserId,
            recoverKeyInputs: CreateValidRecoveryKeyInputs(),
            utcNow: UtcNow
        ).Value;

        Fingerprint fingerprint = CreateValidFingerprint();
        chain.MarkKeyAsUsed("identifier-1", fingerprint, UtcNow);
        chain.MarkKeyAsUsed("identifier-2", fingerprint, UtcNow);

        int count = chain.GetRemainingKeyCount();

        count.Should().Be(RecoveryKeyConstants.MaxKeysPerChain - 2);
    }

    private static Fingerprint CreateValidFingerprint()
    {
        return Fingerprint.Create
        (
            ipAddress: "192.168.1.1",
            userAgent: "Mozilla/5.0",
            timezone: "Europe/London",
            language: "en-US",
            normalizedBrowser: "Chrome 120",
            normalizedOs: "Windows 10"
        ).Value;
    }
}