using FastEndpoints;

using Main.Application.Queries.SharedChats.GetSharedChat;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.SharedChats.Get;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/shared-chats/{sharedChatId}");
        Version(1);
        AllowAnonymous();

        Description(d =>
        {
            d.WithSummary("Get Shared Chat")
                .WithDescription(
                    "Retrieves a publicly shared chat snapshot with all its messages. " +
                    "No authentication required.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.SharedChats);
        });
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        GetSharedChatQuery query = new(request.SharedChatId);

        await SendOutcomeAsync(
            outcome: await _sender.Send(query, ct),
            mapper: response => new Response(
                SharedChat: new SharedChatDto(
                    Id: response.SharedChat.Id,
                    SourceChatId: response.SharedChat.SourceChatId,
                    OwnerId: response.SharedChat.OwnerId,
                    Title: response.SharedChat.Title,
                    ModelId: response.SharedChat.ModelId,
                    ViewCount: response.SharedChat.ViewCount,
                    SnapshotAt: response.SharedChat.SnapshotAt,
                    CreatedAt: response.SharedChat.CreatedAt),
                Messages: response.Messages
                    .Select(m => new SharedChatMessageDto(
                        SequenceNumber: m.SequenceNumber,
                        MessageRole: m.MessageRole,
                        MessageContent: m.MessageContent,
                        AttachmentFileKey: m.AttachmentFileKey,
                        AttachmentContentType: m.AttachmentContentType,
                        AttachmentFileSizeInBytes: m.AttachmentFileSizeInBytes,
                        CreatedAt: m.CreatedAt,
                        EditedAt: m.EditedAt
                        ))
                    .ToList()),
            cancellationToken: ct);
    }
}