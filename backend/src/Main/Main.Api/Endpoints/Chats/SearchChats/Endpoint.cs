using FastEndpoints;

using Main.Application.Queries.Chats.SearchChats;
using Main.Domain.Constants;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Chats.SearchChats;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/chats/search");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Search Chats")
                .WithDescription(
                    "Full-text search across chat titles and message content. " +
                    "Returns matching chats with highlighted snippets.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Chats);
        });
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            await Send.ResponseAsync(new Response([], new PaginationDto(null, false, request.Limit)), cancellation: ct);
            return;
        }

        string sanitizedQuery = request.Query.Length > ChatConstants.MaxSearchQueryLength
            ? request.Query[..ChatConstants.MaxSearchQueryLength]
            : request.Query;

        SearchChatsQuery query = new
        (
            Query: sanitizedQuery,
            Cursor: request.Cursor,
            Limit: Math.Min(request.Limit, ChatConstants.SearchMaxPageSize)
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(query, ct),
            mapper: scr => MapResponse(scr),
            cancellationToken: ct
        );
    }

    private static Response MapResponse(SearchChatsResponse r) => new
    (
        Results: [.. r.Results
            .Select(scrm => new SearchResultDto
            (
                ChatId: scrm.ChatId,
                Title: scrm.Title,
                ModelName: scrm.ModelName,
                Folder: scrm.Folder,
                Snippet: scrm.Snippet,
                MatchedAt: scrm.MatchedAt,
                CreatedAt: scrm.CreatedAt
            ))],
        Pagination: new PaginationDto
        (
            NextCursor: r.PaginationInfo.NextCursor,
            HasMore: r.PaginationInfo.HasMore,
            Limit: r.PaginationInfo.Limit
        )
    );
}