using FastEndpoints;

using Main.Application.Commands.Memories.Update;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Memories.Update;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Patch("/api/memories/{memoryId}");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Update Memory")
                .WithDescription("Updates the content of an existing memory. Regenerates the embedding.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Memories);
        });
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        UpdateMemoryCommand command = new
        (
            MemoryId: request.MemoryId,
            Content: request.Content
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            mapper: r => new Response
            (
                MemoryId: r.MemoryId,
                Content: r.Content,
                UpdatedAt: r.UpdatedAt
            ),
            cancellationToken: ct
        );
    }
}