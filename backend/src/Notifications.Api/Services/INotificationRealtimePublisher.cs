using Notifications.Api.Enums;

namespace Notifications.Api.Services;

internal interface INotificationRealtimePublisher
{
    Task NotificationCreatedAsync
    (
        Guid userId,
        Guid id,
        string category,
        string title,
        string bodyPreview,
        SourceType sourceType,
        string sourceId,
        NotificationStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? readAt,
        CancellationToken cancellationToken = default
    );

    Task NotificationUpdatedAsync
    (
        Guid userId,
        Guid id,
        NotificationStatus status,
        DateTimeOffset? readAt,
        CancellationToken cancellationToken = default
    );
}