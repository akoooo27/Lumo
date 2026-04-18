using FastEndpoints;

using Main.Application.Commands.GoogleConnections.HandleCallback;

using Mediator;

namespace Main.Api.Endpoints.GoogleConnection.HandleCallback;

internal sealed class Endpoint : BaseEndpoint<Request>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender) => _sender = sender;

    public override void Configure()
    {
        Get("/api/google/oauth/callback");
        AllowAnonymous();
        Version(1);

        Description(d =>
        {
            d.WithSummary("Handle Google OAuth Callback")
                .WithDescription("Google redirects here after user consent. Exchanges code for tokens.")
                .Produces(204)
                .ProducesProblemDetails(400)
                .WithTags(CustomTags.GoogleConnections);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        HandleGoogleCallbackCommand command = new
        (
            Code: endpointRequest.Code,
            State: endpointRequest.State
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            cancellationToken: ct
        );
    }
}