using Microsoft.EntityFrameworkCore;

using Notifications.Api.Data;
using Notifications.Api.Data.Entities;
using Notifications.Api.Enums;
using Notifications.Api.Faults;
using Notifications.Api.Services;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Notifications.Api.Commands.Notifications.Update;

internal sealed class UpdateNotificationHandler(
    INotificationDbContext dbContext,
    IUserContext userContext,
    INotificationRealtimePublisher realtimePublisher,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateNotificationCommand, UpdateNotificationResponse>
{
    public async ValueTask<Outcome<UpdateNotificationResponse>> Handle(UpdateNotificationCommand request, CancellationToken cancellationToken)
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

        if (request.Status.HasValue)
        {
            notification.Status = request.Status.Value;

            if (request.Status.Value == NotificationStatus.Read && notification.ReadAt is null)
                notification.ReadAt = dateTimeProvider.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        UpdateNotificationResponse response = new
        (
            Id: notification.Id,
            Status: notification.Status.ToString(),
            ReadAt: notification.ReadAt
        );

        await realtimePublisher.NotificationUpdatedAsync
        (
            userId: userId,
            id: response.Id,
            status: response.Status,
            readAt: response.ReadAt,
            cancellationToken: cancellationToken
        );

        return response;
    }
}