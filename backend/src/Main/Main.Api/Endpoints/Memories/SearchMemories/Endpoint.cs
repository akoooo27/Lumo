using FastEndpoints;

using Main.Application.Abstractions.Memory;
using Main.Application.Queries.Memories.SearchMemories;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Memories.SearchMemories;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/memories/search");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Search Memories")
                .WithDescription("Searches memories for the authenticated user using semantic similarity.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Memories);
        });
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        SearchMemoriesQuery query = new
        (
            Query: request.Query,
            Limit: Math.Clamp(request.Limit, 1, MemoryConstants.MaxPageSize)
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(query, ct),
            mapper: response => new Response
            (
                Memories: response.Memories
                    .Select(m => new MemoryDto
                    (
                        Id: m.Id,
                        Content: m.Content,
                        Category: m.Category,
                        CreatedAt: m.CreatedAt,
                        UpdatedAt: m.UpdatedAt,
                        LastAccessedAt: m.LastAccessedAt,
                        AccessCount: m.AccessCount,
                        Importance: m.Importance
                    ))
                    .ToList()
            ),
            cancellationToken: ct
        );
    }
}