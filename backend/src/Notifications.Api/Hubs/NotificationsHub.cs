using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using Notifications.Api.Constants;

using SharedKernel.Infrastructure.Authentication;

namespace Notifications.Api.Hubs;

[Authorize]
internal sealed class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Guid userId = Context.User!.GetUserId();

        string groupName = $"{NotificationsConstants.UserGroupPrefix}{userId}";

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        await base.OnConnectedAsync();
    }
}