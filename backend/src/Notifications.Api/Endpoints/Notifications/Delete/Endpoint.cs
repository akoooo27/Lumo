using FastEndpoints;

using Mediator;

using Notifications.Api.Commands.Notifications.Delete;

using SharedKernel.Api.Constants;

namespace Notifications.Api.Endpoints.Notifications.Delete;

internal sealed class Endpoint : BaseEndpoint<Request>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Delete("/api/notifications/{notificationId}");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Delete Notification")
                .WithDescription("Permanently deletes a notification.")
                .Produces(204)
                .ProducesProblemDetails(401, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Notifications);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        DeleteNotificationCommand command = new
        (
            NotificationId: endpointRequest.NotificationId
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            cancellationToken: ct
        );
    }
}
