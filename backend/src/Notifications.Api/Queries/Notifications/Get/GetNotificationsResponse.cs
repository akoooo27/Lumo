namespace Notifications.Api.Queries.Notifications.Get;

internal sealed record GetNotificationsResponse(IReadOnlyList<NotificationReadModel> Notifications);