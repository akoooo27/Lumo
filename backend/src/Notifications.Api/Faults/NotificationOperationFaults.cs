using SharedKernel;

namespace Notifications.Api.Faults;

internal static class NotificationOperationFaults
{
    internal static readonly Fault NotFound = Fault.NotFound
    (
        title: "Notification.NotFound",
        detail: "The specified notification was not found."
    );
}