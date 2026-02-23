using FastEndpoints;

using Main.Application.Commands.Preferences.DisableMemory;
using Main.Application.Commands.Preferences.EnableMemory;

using Mediator;

using SharedKernel;
using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Preferences.SetMemory;

internal sealed class Endpoint : BaseEndpoint<Request>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Patch("/api/preferences/memory");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Set Memory Preference")
                .WithDescription("Enables or disables memory persistence for the authenticated user.")
                .Produces(204)
                .ProducesProblemDetails(409, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Preferences);
        });
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        Outcome outcome;

        if (request.Enabled)
            outcome = await _sender.Send(new EnableMemoryCommand(), ct);
        else
            outcome = await _sender.Send(new DisableMemoryCommand(), ct);

        await SendOutcomeAsync
        (
            outcome: outcome,
            cancellationToken: ct
        );
    }
}