using FastEndpoints;

using Main.Application.Commands.Chats.Update;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Chats.Update;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Patch("/api/chats/{chatId}");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Update Chat")
                .WithDescription("Partially updates a chat. Supports renaming, archiving/unarchiving, pinning/unpinning, and folder assignment.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Chats);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        bool hasFolderId = endpointRequest.FolderId is not null;

        UpdateChatCommand command = new
        (
            ChatId: endpointRequest.ChatId,
            NewTitle: endpointRequest.NewTitle,
            IsArchived: endpointRequest.IsArchived,
            IsPinned: endpointRequest.IsPinned,
            FolderId: endpointRequest.FolderId,
            HasFolderId: hasFolderId
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            cancellationToken: ct,
            mapper: ucr => new Response
            (
                ChatId: ucr.ChatId,
                Title: ucr.Title,
                FolderId: ucr.FolderId,
                IsArchived: ucr.IsArchived,
                IsPinned: ucr.IsPinned,
                UpdatedAt: ucr.UpdatedAt
            )
        );
    }
}