using FastEndpoints;

using Main.Application.Queries.Memories.GetMemoryById;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Memories.GetById;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender) => _sender = sender;

    public override void Configure()
    {
        Get("/api/memories/{memoryId}");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Memory By Id")
                .WithDescription("Retrieves a single memory by its ID for the authenticated user.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Memories);
        });
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        GetMemoryByIdQuery query = new(request.MemoryId);

        await SendOutcomeAsync(
            outcome: await _sender.Send(query, ct),
            mapper: r => new Response(
                Id: r.Id,
                Content: r.Content,
                Category: r.Category,
                CreatedAt: r.CreatedAt,
                UpdatedAt: r.UpdatedAt,
                LastAccessedAt: r.LastAccessedAt,
                AccessCount: r.AccessCount,
                Importance: r.Importance),
            cancellationToken: ct);
    }
}