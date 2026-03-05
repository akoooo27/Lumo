using Notifications.Api.Enums;

namespace Notifications.Api.Endpoints.Notifications.GetNotifications;

internal sealed record NotificationDto
(
    Guid Id,
    string Category,
    string Title,
    string BodyPreview,
    SourceType SourceType,
    string SourceId,
    NotificationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt
);

internal sealed record Response(IReadOnlyList<NotificationDto> Notifications);