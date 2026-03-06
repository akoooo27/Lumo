using Microsoft.AspNetCore.SignalR;

using Notifications.Api.Constants;
using Notifications.Api.Enums;
using Notifications.Api.Hubs;

namespace Notifications.Api.Services;

internal sealed class NotificationRealtimePublisher(
    IHubContext<NotificationsHub> hubContext,
    ILogger<NotificationRealtimePublisher> logger) : INotificationRealtimePublisher
{
    public async Task NotificationCreatedAsync
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
    )
    {
        try
        {
            await hubContext.Clients
                .Group($"{NotificationsConstants.UserGroupPrefix}{userId}")
                .SendAsync
                (
                    method: $"{NotificationsConstants.NotificationCreatedMethod}",
                    arg1: new
                    {
                        Id = id,
                        Category = category,
                        Title = title,
                        BodyPreview = bodyPreview,
                        SourceType = sourceType,
                        SourceId = sourceId,
                        Status = status,
                        CreatedAt = createdAt,
                        ReadAt = readAt
                    },
                    cancellationToken
                );
        }
#pragma warning disable CA1031
        catch (Exception exception)
#pragma warning restore CA1031
        {
            logger.LogWarning(exception, "Failed to push notification.created to user {UserId}", userId);
        }
    }

    public async Task NotificationUpdatedAsync
    (
        Guid userId,
        Guid id,
        NotificationStatus status,
        DateTimeOffset? readAt,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await hubContext.Clients
                .Group($"{NotificationsConstants.UserGroupPrefix}{userId}")
                .SendAsync
                (
                    method: $"{NotificationsConstants.NotificationUpdatedMethod}",
                    arg1: new
                    {
                        Id = id,
                        Status = status,
                        ReadAt = readAt
                    },
                    cancellationToken
                );
        }
#pragma warning disable CA1031
        catch (Exception exception)
#pragma warning restore CA1031
        {
            logger.LogWarning(exception, "Failed to push notification.updated to user {UserId}", userId);
        }
    }
}