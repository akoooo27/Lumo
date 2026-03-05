using Notifications.Api.Enums;

namespace Notifications.Api.Endpoints.Notifications.Patch;

internal sealed record Request
(
    Guid NotificationId,
    NotificationStatus? Status
);