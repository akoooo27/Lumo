using System.Globalization;
using System.Text.Json;

using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

using Contracts.IntegrationEvents.Workflow;

using Microsoft.Extensions.Options;

using Notifications.Api.Models;
using Notifications.Api.Options;

namespace Notifications.Api.Services;

internal sealed class SesEmailService(
    IAmazonSimpleEmailServiceV2 sesService,
    IOptions<EmailOptions> emailOptions,
    ILogger<SesEmailService> logger) : IEmailService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly EmailOptions _emailOptions = emailOptions.Value;

    public async Task SendTemplatedEmailAsync<TData>(string recipientEmailAddress, string templateName, TData templateData,
        CancellationToken cancellationToken = default) where TData : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmailAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentNullException.ThrowIfNull(templateData);

        string templateDataJson = JsonSerializer.Serialize(templateData, JsonOptions);

        SendEmailRequest request = new()
        {
            FromEmailAddress = _emailOptions.SenderEmail,
            Destination = new Destination()
            {
                ToAddresses = [recipientEmailAddress]
            },
            Content = new EmailContent()
            {
                Template = new Template()
                {
                    TemplateName = templateName,
                    TemplateData = templateDataJson
                }
            }
        };

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Sending templated email to recipient. Template: {TemplateName}", templateName);

        SendEmailResponse response = await sesService.SendEmailAsync(request, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Templated email sent using template {TemplateName}. MessageId: {MessageId}, HttpStatusCode: {StatusCode}",
                templateName, response.MessageId, response.HttpStatusCode);
    }

    public Task SendWorkflowNotificationAsync(
        WorkflowRunNotificationRequested message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.RecipientEmailAddress);

        string viewResultUrl = $"{_emailOptions.FrontendUrl}/workflows/{message.WorkflowId}/runs/{message.WorkflowRunId}";
        string manageWorkflowsUrl = $"{_emailOptions.FrontendUrl}/workflows";

        return message.Category switch
        {
            WorkflowNotificationCategory.WorkflowSucceeded => SendTemplatedEmailAsync
            (
                recipientEmailAddress: message.RecipientEmailAddress,
                templateName: _emailOptions.WorkflowRunSucceededTemplateName,
                templateData: new WorkflowRunSucceededTemplateData
                {
                    WorkflowTitle = message.Title,
                    ResultPreview = message.BodyPreview,
                    ViewResultUrl = viewResultUrl,
                    ManageWorkflowsUrl = manageWorkflowsUrl,
                    NextRunAt = message.NextRunAt?.ToString("f", CultureInfo.InvariantCulture) ?? "Not scheduled",
                    ApplicationName = _emailOptions.ApplicationName
                },
                cancellationToken: cancellationToken
            ),

            WorkflowNotificationCategory.WorkflowFailed => SendTemplatedEmailAsync
            (
                recipientEmailAddress: message.RecipientEmailAddress,
                templateName: _emailOptions.WorkflowRunFailedTemplateName,
                templateData: new WorkflowRunFailedTemplateData
                {
                    WorkflowTitle = message.Title,
                    FailureMessage = message.BodyPreview,
                    StatusMessage = "Your workflow will continue to run on its next scheduled occurrence.",
                    ViewResultUrl = viewResultUrl,
                    ManageWorkflowsUrl = manageWorkflowsUrl,
                    ApplicationName = _emailOptions.ApplicationName
                },
                cancellationToken: cancellationToken
            ),

            WorkflowNotificationCategory.WorkflowPaused => SendTemplatedEmailAsync
            (
                recipientEmailAddress: message.RecipientEmailAddress,
                templateName: _emailOptions.WorkflowRunFailedTemplateName,
                templateData: new WorkflowRunFailedTemplateData
                {
                    WorkflowTitle = message.Title,
                    FailureMessage = message.BodyPreview,
                    StatusMessage = "Your workflow has been automatically paused due to repeated failures. To resume it, visit your workflow settings.",
                    ViewResultUrl = viewResultUrl,
                    ManageWorkflowsUrl = manageWorkflowsUrl,
                    ApplicationName = _emailOptions.ApplicationName
                },
                cancellationToken: cancellationToken
            ),

            _ => Task.CompletedTask
        };
    }
}