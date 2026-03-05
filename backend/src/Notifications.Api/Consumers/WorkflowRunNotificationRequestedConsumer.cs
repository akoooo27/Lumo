using Amazon.SimpleEmailV2;

using Contracts.IntegrationEvents.Workflow;

using MassTransit;

using Microsoft.EntityFrameworkCore;

using Notifications.Api.Data;
using Notifications.Api.Data.Entities;
using Notifications.Api.Enums;
using Notifications.Api.Services;

namespace Notifications.Api.Consumers;

internal sealed class WorkflowRunNotificationRequestedConsumer(
    INotificationDbContext dbContext,
    INotificationRealtimePublisher realtimePublisher,
    IEmailService emailService,
    ILogger<WorkflowRunNotificationRequestedConsumer> logger) : IConsumer<WorkflowRunNotificationRequested>
{
    public async Task Consume(ConsumeContext<WorkflowRunNotificationRequested> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        WorkflowRunNotificationRequested message = context.Message;

        bool exists = await dbContext.Notifications
            .AnyAsync(n => n.Identifier == message.IdempotencyId, cancellationToken);

        if (exists)
            return;

        Notification notification = new()
        {
            Id = Guid.NewGuid(),
            UserId = message.UserId,
            Identifier = message.IdempotencyId,
            Category = message.Category.ToString(),
            Title = message.Title,
            BodyPreview = message.BodyPreview,
            SourceType = SourceType.WorkFlowRun,
            SourceId = message.WorkflowRunId,
            Status = NotificationStatus.Unread,
            EmailStatus = EmailStatus.NotSent,
            CreatedAt = message.OccurredAt
        };

        await dbContext.Notifications.AddAsync(notification, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await realtimePublisher.NotificationCreatedAsync
        (
            userId: notification.UserId,
            id: notification.Id,
            category: notification.Category,
            title: notification.Title,
            bodyPreview: notification.BodyPreview,
            sourceType: notification.SourceType,
            sourceId: notification.SourceId,
            status: notification.Status,
            createdAt: notification.CreatedAt,
            readAt: notification.ReadAt,
            cancellationToken: cancellationToken
        );

        if (string.IsNullOrEmpty(message.RecipientEmailAddress))
            return;

        try
        {
            await emailService.SendWorkflowNotificationAsync(message, cancellationToken);

            notification.EmailStatus = EmailStatus.Sent;
        }
        catch (AmazonSimpleEmailServiceV2Exception ex)
        {
            logger.LogError(ex,
                "Failed to send workflow notification email. Category={Category}, WorkflowRunId={WorkflowRunId}",
                message.Category, message.WorkflowRunId);

            notification.EmailStatus = EmailStatus.Failed;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}