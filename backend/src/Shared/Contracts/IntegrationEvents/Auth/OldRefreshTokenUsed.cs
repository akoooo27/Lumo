namespace Contracts.IntegrationEvents.Auth;

public sealed record OldRefreshTokenUsed
{
    public required Guid EventId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public required Guid CorrelationId { get; init; }

    public required Guid UserId { get; init; }

    public required string EmailAddress { get; init; }

    public required string IpAddress { get; init; }

    public required string UserAgent { get; init; }
};