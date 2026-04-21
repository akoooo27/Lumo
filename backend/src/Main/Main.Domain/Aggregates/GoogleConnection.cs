using System.Diagnostics.CodeAnalysis;

using Main.Domain.Constants;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Aggregates;

public sealed class GoogleConnection : AggregateRoot<GoogleConnectionId>
{
    public Guid UserId { get; private set; }

    public string GoogleEmail { get; private set; } = string.Empty;

    public string ProtectedAccessToken { get; private set; } = string.Empty;

    public string ProtectedRefreshToken { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    private GoogleConnection() { } // For EF Core

    [SetsRequiredMembers]
    private GoogleConnection
    (
        GoogleConnectionId id,
        Guid userId,
        string googleEmail,
        string protectedAccessToken,
        string protectedRefreshToken,
        DateTimeOffset utcNow,
        DateTimeOffset expiresAt
    )
    {
        Id = id;
        UserId = userId;
        GoogleEmail = googleEmail;
        ProtectedAccessToken = protectedAccessToken;
        ProtectedRefreshToken = protectedRefreshToken;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
        ExpiresAt = expiresAt;
    }

    public static Outcome<GoogleConnection> Create
    (
        GoogleConnectionId id,
        Guid userId,
        string googleEmail,
        string protectedAccessToken,
        string protectedRefreshToken,
        DateTimeOffset utcNow,
        DateTimeOffset expiresAt
    )
    {
        if (userId == Guid.Empty)
            return GoogleConnectionFaults.UserIdRequired;

        if (string.IsNullOrWhiteSpace(googleEmail))
            return GoogleConnectionFaults.GoogleEmailRequired;

        if (string.IsNullOrWhiteSpace(protectedAccessToken))
            return GoogleConnectionFaults.AccessTokenRequired;

        if (string.IsNullOrWhiteSpace(protectedRefreshToken))
            return GoogleConnectionFaults.RefreshTokenRequired;

        GoogleConnection connection = new
        (
            id: id,
            userId: userId,
            googleEmail: googleEmail,
            protectedAccessToken: protectedAccessToken,
            protectedRefreshToken: protectedRefreshToken,
            utcNow: utcNow,
            expiresAt: expiresAt
        );

        return connection;
    }

    public Outcome UpdateTokens
    (
        string protectedAccessToken,
        string protectedRefreshToken,
        DateTimeOffset utcNow,
        DateTimeOffset expiresAt
    )
    {
        if (string.IsNullOrWhiteSpace(protectedAccessToken))
            return GoogleConnectionFaults.AccessTokenRequired;

        if (string.IsNullOrWhiteSpace(protectedRefreshToken))
            return GoogleConnectionFaults.RefreshTokenRequired;

        ProtectedAccessToken = protectedAccessToken;
        ProtectedRefreshToken = protectedRefreshToken;
        UpdatedAt = utcNow;
        ExpiresAt = expiresAt;

        return Outcome.Success();
    }

    public bool IsTokenExpired(DateTimeOffset utcNow) =>
        utcNow >= ExpiresAt.AddMinutes(-GoogleConnectionConstants.TokenRefreshBufferMinutes);
}