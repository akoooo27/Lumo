using FastEndpoints;

using Main.Application.Abstractions.Memory;
using Main.Application.Queries.Memories.GetMemories;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Memories.GetMemories;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/memories");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Memories")
                .WithDescription(
                    "Retrieves paginated memories for the authenticated user. " +
                    "Memories are returned in reverse chronological order. " +
                    "Optionally filter by category.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Memories);
        });
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        GetMemoriesQuery query = new
        (
            Category: request.Category,
            Cursor: request.Cursor,
            Limit: Math.Min(request.Limit, MemoryConstants.MaxPageSize)
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
                    .ToList(),
                Pagination: new PaginationDto
                (
                    NextCursor: response.PaginationInfo.NextCursor,
                    HasMore: response.PaginationInfo.HasMore,
                    Limit: response.PaginationInfo.Limit
                )
            ),
            cancellationToken: ct
        );
    }
}