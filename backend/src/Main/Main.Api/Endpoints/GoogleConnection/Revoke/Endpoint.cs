using FastEndpoints;

using Main.Application.Commands.GoogleConnections.Revoke;

using Mediator;

namespace Main.Api.Endpoints.GoogleConnection.Revoke;

internal sealed class Endpoint : BaseEndpoint<EmptyRequest>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender) => _sender = sender;

    public override void Configure()
    {
        Delete("/api/google/connection");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Revoke Google Connection")
                .WithDescription("Revokes the Google OAuth connection and deletes stored tokens.")
                .Produces(204)
                .ProducesProblemDetails(404)
                .WithTags(CustomTags.GoogleConnections);
        });
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        RevokeGoogleConnectionCommand command = new();

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            cancellationToken: ct
        );
    }
}