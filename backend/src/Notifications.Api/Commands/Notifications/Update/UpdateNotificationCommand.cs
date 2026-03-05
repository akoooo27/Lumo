using Notifications.Api.Enums;

using SharedKernel.Application.Messaging;

namespace Notifications.Api.Commands.Notifications.Update;

internal sealed record UpdateNotificationCommand(Guid NotificationId, NotificationStatus? Status) : ICommand<UpdateNotificationResponse>;