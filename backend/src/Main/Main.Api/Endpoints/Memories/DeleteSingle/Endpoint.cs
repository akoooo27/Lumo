using FastEndpoints;

using Main.Application.Commands.Memories.DeleteSingle;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Memories.DeleteSingle;

internal sealed class Endpoint : BaseEndpoint<Request>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Delete("/api/memories/{memoryId}");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Delete Memory")
                .WithDescription("Soft-deletes a single memory for the authenticated user.")
                .Produces(204)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Memories);
        });
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        DeleteMemoryCommand command = new(request.MemoryId);

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            cancellationToken: ct
        );
    }
}