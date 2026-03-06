using Contracts.IntegrationEvents.Auth;

using MassTransit;

using Microsoft.Extensions.Options;

using Notifications.Api.Models;
using Notifications.Api.Options;
using Notifications.Api.Services;

namespace Notifications.Api.Consumers.Audit;

internal sealed class RecoveryInitiatedAuditConsumer(
    IEmailService emailService,
    IOptions<EmailOptions> emailOptions,
    ILogger<RecoveryInitiatedAuditConsumer> logger) : IConsumer<RecoveryInitiated>
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    public async Task Consume(ConsumeContext<RecoveryInitiated> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        RecoveryInitiated message = context.Message;

        RecoveryInitiatedTemplateData templateData = new()
        {
            OldEmailAddress = message.OldEmailAddress,
            NewEmailAddress = message.NewEmailAddress,
            OtpToken = message.OtpToken,
            MagicLinkToken = message.MagicLinkToken,
            ExpiresAt = message.ExpiresAt,
            IpAddress = message.IpAddress,
            UserAgent = message.UserAgent,
            ApplicationName = _emailOptions.ApplicationName,
            FrontendUrl = _emailOptions.FrontendUrl
        };

        await emailService.SendTemplatedEmailAsync
        (
            recipientEmailAddress: message.NewEmailAddress,
            templateName: _emailOptions.RecoveryInitiatedTemplateName,
            templateData: templateData,
            cancellationToken: cancellationToken
        );

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}, UserId: {UserId}",
                nameof(RecoveryInitiated), message.EventId, message.CorrelationId, message.OccurredAt, message.UserId);
    }
}