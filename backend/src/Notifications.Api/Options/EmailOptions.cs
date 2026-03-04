using System.ComponentModel.DataAnnotations;

namespace Notifications.Api.Options;

internal sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required, EmailAddress]
    public string SenderEmail { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string ApplicationName { get; init; } = string.Empty;

    [Required, Url]
    public string FrontendUrl { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string WelcomeEmailTemplateName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string LoginRequestedTemplateName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string LoginVerifiedTemplateName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string EmailAddressChangedTemplateName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string RecoveryInitiatedTemplateName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string EmailChangeRequestedTemplateName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string UserDeletionRequestedTemplateName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string UserDeletionCanceledTemplateName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string UserDeletedTemplateName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string WorkflowRunSucceededTemplateName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string WorkflowRunFailedTemplateName { get; init; } = string.Empty;
}