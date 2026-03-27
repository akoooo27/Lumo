using SharedKernel.Application.Messaging;

namespace Notifications.Api.Commands.Notifications.Delete;

internal sealed record DeleteNotificationCommand(Guid NotificationId) : ICommand;