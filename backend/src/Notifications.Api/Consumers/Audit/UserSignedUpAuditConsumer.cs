using Contracts.IntegrationEvents.Auth;

using MassTransit;

using Microsoft.Extensions.Options;

using Notifications.Api.Models;
using Notifications.Api.Options;
using Notifications.Api.Services;

namespace Notifications.Api.Consumers.Audit;

internal sealed class UserSignedUpAuditConsumer(
    IEmailService emailService,
    IOptions<EmailOptions> emailOptions,
    ILogger<UserSignedUpAuditConsumer> logger) : IConsumer<UserSignedUp>
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    public async Task Consume(ConsumeContext<UserSignedUp> context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        UserSignedUp message = context.Message;

        WelcomeEmailTemplateData templateData = new()
        {
            DisplayName = message.DisplayName,
            ApplicationName = _emailOptions.ApplicationName
        };

        await emailService.SendTemplatedEmailAsync
        (
            recipientEmailAddress: message.EmailAddress,
            templateName: _emailOptions.WelcomeEmailTemplateName,
            templateData: templateData,
            cancellationToken: cancellationToken
        );

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Consumed {EventType}: {EventId}, CorrelationId: {CorrelationId}, OccurredAt: {OccurredAt}, UserId: {UserId}",
                nameof(UserSignedUp), message.EventId, message.CorrelationId, message.OccurredAt, message.UserId);
    }
}