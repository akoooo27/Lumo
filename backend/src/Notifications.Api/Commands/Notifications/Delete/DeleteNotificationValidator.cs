using FluentValidation;

namespace Notifications.Api.Commands.Notifications.Delete;

internal sealed class DeleteNotificationValidator : AbstractValidator<DeleteNotificationCommand>
{
    public DeleteNotificationValidator()
    {
        RuleFor(dnc => dnc.NotificationId)
            .NotEmpty().WithMessage("Notification ID is required");
    }
}