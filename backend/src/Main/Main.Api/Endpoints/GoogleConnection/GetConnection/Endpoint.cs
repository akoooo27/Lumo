using FastEndpoints;

using Main.Application.Queries.GoogleConnections.GetConnection;

using Mediator;

namespace Main.Api.Endpoints.GoogleConnection.GetConnection;

internal sealed class Endpoint : BaseEndpoint<EmptyRequest, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender) => _sender = sender;

    public override void Configure()
    {
        Get("/api/google/connection");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Google Connection")
                .WithDescription("Returns the Google connection status for the current user.")
                .Produces<Response>(200)
                .ProducesProblemDetails(401)
                .WithTags(CustomTags.GoogleConnections);
        });
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        GetGoogleConnectionQuery query = new();

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(query, ct),
            mapper: r => new Response
            (
                IsConnected: r.IsConnected,
                GoogleEmail: r.GoogleEmail
            ),
            cancellationToken: ct
        );
    }
}