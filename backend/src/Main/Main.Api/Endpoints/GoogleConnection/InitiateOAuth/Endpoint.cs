using FastEndpoints;

using Main.Application.Commands.GoogleConnections.InitiateOAuth;

using Mediator;

namespace Main.Api.Endpoints.GoogleConnection.InitiateOAuth;

internal sealed class Endpoint : BaseEndpoint<EmptyRequest, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender) => _sender = sender;

    public override void Configure()
    {
        Get("/api/google/oauth/initiate");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Initiate Google OAuth")
                .WithDescription("Returns the Google OAuth consent URL for the current user.")
                .Produces<Response>(200)
                .ProducesProblemDetails(401)
                .WithTags(CustomTags.GoogleConnections);
        });
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        InitiateGoogleOAuthCommand command = new();

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            mapper: r => new Response(RedirectUrl: r.RedirectUrl),
            cancellationToken: ct
        );
    }
}