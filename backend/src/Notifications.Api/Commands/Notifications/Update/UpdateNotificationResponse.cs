namespace Notifications.Api.Commands.Notifications.Update;

internal sealed record UpdateNotificationResponse
(
    Guid Id,
    string Status,
    DateTimeOffset? ReadAt
);