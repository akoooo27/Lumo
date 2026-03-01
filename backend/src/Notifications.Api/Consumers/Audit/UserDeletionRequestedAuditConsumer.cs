using System.Globalization;

using Contracts.IntegrationEvents.Auth;

using MassTransit;

using Microsoft.Extensions.Options;

using Notifications.Api.Models;
using Notifications.Api.Options;
using Notifications.Api.Services;

namespace Notifications.Api.Consumers.Audit;

internal sealed class UserDeletionRequestedAuditConsumer(
    IEmailService emailService,
    IOptions<EmailOptions> emailOptions,
    ILogger<UserDeletionRequestedAuditConsumer> logger) : IConsumer<UserDeletionRequested>
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    public async Task Consume(ConsumeContext<UserDeletionRequested> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        UserDeletionRequested message = context.Message;

        UserDeletionRequestedTemplateData templateData = new()
        {
            WillBeDeletedAt = message.WillBeDeletedAt.UtcDateTime.ToString("MMMM dd, yyyy 'at' HH:mm 'UTC'", CultureInfo.InvariantCulture),
            IpAddress = message.IpAddress,
            UserAgent = message.UserAgent,
            ApplicationName = _emailOptions.ApplicationName
        };

        await emailService.SendTemplatedEmailAsync
        (
            recipientEmailAddress: message.EmailAddress,
            templateName: _emailOptions.UserDeletionRequestedTemplateName,
            templateData: templateData,
            cancellationToken: cancellationToken
        );

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}, UserId: {UserId}",
                nameof(UserDeletionRequested), message.EventId, message.CorrelationId, message.OccurredAt, message.UserId);
    }
}