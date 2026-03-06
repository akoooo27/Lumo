namespace Notifications.Api.Models;

internal sealed record WorkflowRunFailedTemplateData
{
    public required string WorkflowTitle { get; init; }

    public required string FailureMessage { get; init; }

    public required string StatusMessage { get; init; }

    public required string ViewResultUrl { get; init; }

    public required string ManageWorkflowsUrl { get; init; }

    public required string ApplicationName { get; init; }
}