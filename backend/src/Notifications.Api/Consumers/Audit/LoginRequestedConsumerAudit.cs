using Contracts.IntegrationEvents.Auth;

using MassTransit;

using Microsoft.Extensions.Options;

using Notifications.Api.Models;
using Notifications.Api.Options;
using Notifications.Api.Services;

namespace Notifications.Api.Consumers.Audit;

internal sealed class LoginRequestedConsumerAudit(
    IEmailService emailService,
    IOptions<EmailOptions> emailOptions,
    ILogger<LoginRequestedConsumerAudit> logger) : IConsumer<LoginRequested>
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    public async Task Consume(ConsumeContext<LoginRequested> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        LoginRequested message = context.Message;

        LoginRequestedEmailTemplateData templateData = new()
        {
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
            recipientEmailAddress: message.EmailAddress,
            templateName: _emailOptions.LoginRequestedTemplateName,
            templateData: templateData,
            cancellationToken: cancellationToken
        );

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}, Email: {Email}",
                nameof(LoginRequested), message.EventId, message.CorrelationId, message.OccurredAt, message.EmailAddress);
    }
}