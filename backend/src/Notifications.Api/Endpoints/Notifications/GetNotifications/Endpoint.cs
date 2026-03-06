using FastEndpoints;

using Mediator;

using Notifications.Api.Queries.Notifications.Get;

using SharedKernel.Api.Constants;
using SharedKernel.Api.Infrastructure;

namespace Notifications.Api.Endpoints.Notifications.GetNotifications;

internal sealed class Endpoint : EndpointWithoutRequest<Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/notifications");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Notifications")
                .WithDescription("Retrieves all active notifications for the current user.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Notifications);
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        GetNotificationsQuery query = new();

        var outcome = await _sender.Send(query, ct);

        if (outcome.IsFailure)
        {
            await Send.ResultAsync(CustomResults.Problem(outcome, HttpContext));
            return;
        }

        Response response = new
        (
            Notifications: outcome.Value.Notifications
                .Select(n => new NotificationDto
                (
                    Id: n.Id,
                    Category: n.Category,
                    Title: n.Title,
                    BodyPreview: n.BodyPreview,
                    SourceType: n.SourceType,
                    SourceId: n.SourceId,
                    Status: n.Status,
                    CreatedAt: n.CreatedAt,
                    ReadAt: n.ReadAt
                ))
                .ToList()
        );

        await Send.ResponseAsync(response, cancellation: ct);
    }
}