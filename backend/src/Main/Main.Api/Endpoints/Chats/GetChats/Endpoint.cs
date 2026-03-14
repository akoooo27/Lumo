using FastEndpoints;

using Main.Application.Queries.Chats.GetChats;
using Main.Domain.Constants;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Chats.GetChats;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/chats");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Chats")
                .WithDescription(
                    "Retrieves paginated chats for the authenticated user. " +
                    "Chats are returned in reverse chronological order (newest first). " +
                    "Use the cursor parameter to load older chats.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Chats);
        });
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        GetChatsQuery query = new
        (
            Cursor: request.Cursor,
            Limit: Math.Min(request.Limit, ChatConstants.MaxPageSize),
            FolderId: request.FolderId,
            HasFolderId: request.FolderId is not null
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(query, ct),
            mapper: response => new Response
            (
                Chats: response.Chats
                    .Select(c => new ChatDto
                    (
                        Id: c.Id,
                        Title: c.Title,
                        ModelName: c.ModelName,
                        FolderId: c.FolderId,
                        IsArchived: c.IsArchived,
                        IsPinned: c.IsPinned,
                        CreatedAt: c.CreatedAt,
                        UpdatedAt: c.UpdatedAt,
                        MessagesCount: c.MessagesCount
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