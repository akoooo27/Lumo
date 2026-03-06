using SharedKernel.Application.Messaging;

namespace Notifications.Api.Queries.Notifications.Get;

internal sealed record GetNotificationsQuery : IQuery<GetNotificationsResponse>;