namespace Notifications.Api.Models;

internal sealed record WorkflowRunSucceededTemplateData
{
    public required string WorkflowTitle { get; init; }

    public required string ResultPreview { get; init; }

    public required string ViewResultUrl { get; init; }

    public required string ManageWorkflowsUrl { get; init; }

    public required string NextRunAt { get; init; }

    public required string ApplicationName { get; init; }
}