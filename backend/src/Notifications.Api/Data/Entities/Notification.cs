using Notifications.Api.Enums;

namespace Notifications.Api.Data.Entities;

internal sealed class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid Identifier { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string BodyPreview { get; set; } = string.Empty;

    public SourceType SourceType { get; set; }

    public string SourceId { get; set; } = string.Empty;

    public NotificationStatus Status { get; set; } = NotificationStatus.Unread;

    public EmailStatus EmailStatus { get; set; } = EmailStatus.NotSent;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ReadAt { get; set; }
}