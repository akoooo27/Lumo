using FastEndpoints;

using Mediator;

using Notifications.Api.Commands.Notifications.Update;

using SharedKernel.Api.Constants;

namespace Notifications.Api.Endpoints.Notifications.Patch;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Patch("/api/notifications/{notificationId}");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Patch Notification")
                .WithDescription("Partially updates a notification. Supports marking as read.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Notifications);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        UpdateNotificationCommand command = new
        (
            NotificationId: endpointRequest.NotificationId,
            Status: endpointRequest.Status
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            mapper: r => new Response
            (
                Id: r.Id,
                Status: r.Status,
                ReadAt: r.ReadAt
            ),
            cancellationToken: ct
        );
    }
}