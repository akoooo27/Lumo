using FastEndpoints;

using Main.Application.Queries.Chats.GetMessages;
using Main.Domain.Constants;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Chats.GetMessages;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/chats/{chatId}/messages");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Messages")
                .WithDescription(
                    "Retrieves paginated messages for a chat. " +
                    "Messages are returned in chronological order (oldest first). " +
                    "Use the cursor parameter to load older messages.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Chats);
        });
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        GetMessagesQuery query = new
        (
            ChatId: request.ChatId,
            Cursor: request.Cursor,
            Limit: Math.Min(request.Limit, MessageConstants.MaxPageSize)
        );

        await SendOutcomeAsync(
            outcome: await _sender.Send(query, ct),
            mapper: response => new Response(
                Messages: response.Messages
                    .Select(mrm => new MessageDto
                    (
                        Id: mrm.Id,
                        ChatId: mrm.ChatId,
                        MessageContent: mrm.MessageContent,
                        MessageRole: mrm.MessageRole,
                        InputTokenCount: mrm.InputTokenCount,
                        OutputTokenCount: mrm.OutputTokenCount,
                        TotalTokenCount: mrm.TotalTokenCount,
                        SequenceNumber: mrm.SequenceNumber,
                        SourcesJson: mrm.SourcesJson,
                        AttachmentFileKey: mrm.AttachmentFileKey,
                        AttachmentContentType: mrm.AttachmentContentType,
                        AttachmentFileSizeInBytes: mrm.AttachmentFileSizeInBytes,
                        CreatedAt: mrm.CreatedAt,
                        EditedAt: mrm.EditedAt
                    ))
                    .ToList(),
                Pagination: new PaginationDto
                (
                    NextCursor: response.Pagination.NextCursor,
                    HasMore: response.Pagination.HasMore,
                    Limit: response.Pagination.Limit
                )
            ),
            successStatusCode: 200,
            cancellationToken: ct
        );
    }
}