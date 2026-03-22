using Microsoft.EntityFrameworkCore;

using Notifications.Api.Data;
using Notifications.Api.Data.Entities;
using Notifications.Api.Faults;
using Notifications.Api.Services;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Notifications.Api.Commands.Notifications.Delete;

internal sealed class DeleteNotificationHandler(
    INotificationDbContext dbContext,
    IUserContext userContext,
    INotificationRealtimePublisher realtimePublisher) : ICommandHandler<DeleteNotificationCommand>
{
    public async ValueTask<Outcome> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        bool userExists = await dbContext.Users
            .AnyAsync(u => u.UserId == userId, cancellationToken);

        if (!userExists)
            return UserOperationFaults.NotFound;

        Notification? notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == userId, cancellationToken);

        if (notification is null)
            return NotificationOperationFaults.NotFound;

        dbContext.Notifications.Remove(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        await realtimePublisher.NotificationDeletedAsync
        (
            userId: userId,
            id: notification.Id,
            cancellationToken: cancellationToken
        );

        return Outcome.Success();
    }
}