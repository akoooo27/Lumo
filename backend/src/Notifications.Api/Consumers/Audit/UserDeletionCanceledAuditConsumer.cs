using Contracts.IntegrationEvents.Auth;

using MassTransit;

using Microsoft.Extensions.Options;

using Notifications.Api.Models;
using Notifications.Api.Options;
using Notifications.Api.Services;

namespace Notifications.Api.Consumers.Audit;

internal sealed class UserDeletionCanceledAuditConsumer(
    IEmailService emailService,
    IOptions<EmailOptions> emailOptions,
    ILogger<UserDeletionCanceledAuditConsumer> logger) : IConsumer<UserDeletionCanceled>
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    public async Task Consume(ConsumeContext<UserDeletionCanceled> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        UserDeletionCanceled message = context.Message;

        UserDeletionCanceledTemplateData templateData = new()
        {
            ApplicationName = _emailOptions.ApplicationName
        };

        await emailService.SendTemplatedEmailAsync
        (
            recipientEmailAddress: message.EmailAddress,
            templateName: _emailOptions.UserDeletionCanceledTemplateName,
            templateData: templateData,
            cancellationToken: cancellationToken
        );

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}, UserId: {UserId}",
                nameof(UserDeletionCanceled), message.EventId, message.CorrelationId, message.OccurredAt, message.UserId);
    }
}