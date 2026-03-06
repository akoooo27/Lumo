using Contracts.IntegrationEvents.Auth;

using MassTransit;

using Microsoft.Extensions.Options;

using Notifications.Api.Models;
using Notifications.Api.Options;
using Notifications.Api.Services;

namespace Notifications.Api.Consumers.Audit;

internal sealed class EmailChangeRequestedAuditConsumer(
    IEmailService emailService,
    IOptions<EmailOptions> emailOptions,
    ILogger<EmailChangeRequestedAuditConsumer> logger) : IConsumer<EmailChangeRequested>
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    public async Task Consume(ConsumeContext<EmailChangeRequested> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        EmailChangeRequested message = context.Message;

        EmailChangeRequestedTemplateData templateData = new()
        {
            CurrentEmailAddress = message.CurrentEmailAddress,
            NewEmailAddress = message.NewEmailAddress,
            OtpToken = message.OtpToken,
            ExpiresAt = message.ExpiresAt,
            IpAddress = message.IpAddress,
            UserAgent = message.UserAgent,
            ApplicationName = _emailOptions.ApplicationName
        };

        await emailService.SendTemplatedEmailAsync
        (
            recipientEmailAddress: message.NewEmailAddress,
            templateName: _emailOptions.EmailChangeRequestedTemplateName,
            templateData: templateData,
            cancellationToken: cancellationToken
        );

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}, UserId: {UserId}",
                nameof(EmailChangeRequested), message.EventId, message.CorrelationId, message.OccurredAt, message.UserId);
    }
}