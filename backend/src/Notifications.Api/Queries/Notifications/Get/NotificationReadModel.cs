using Notifications.Api.Enums;

namespace Notifications.Api.Queries.Notifications.Get;

internal sealed record class NotificationReadModel
{
    public required Guid Id { get; init; }

    public required string Category { get; init; }

    public required string Title { get; init; }

    public required string BodyPreview { get; init; }

    public SourceType SourceType { get; init; }

    public required string SourceId { get; init; }

    public NotificationStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ReadAt { get; init; }
}