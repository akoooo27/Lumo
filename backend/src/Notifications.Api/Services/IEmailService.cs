using Contracts.IntegrationEvents.Workflow;

namespace Notifications.Api.Services;

internal interface IEmailService
{
    Task SendTemplatedEmailAsync<TData>
    (
        string recipientEmailAddress,
        string templateName,
        TData templateData,
        CancellationToken cancellationToken = default
    ) where TData : notnull;

    Task SendWorkflowNotificationAsync
    (
        WorkflowRunNotificationRequested message,
        CancellationToken cancellationToken = default
    );
}