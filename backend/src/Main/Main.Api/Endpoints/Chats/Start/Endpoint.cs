using FastEndpoints;

using Main.Application.Abstractions.Storage;
using Main.Application.Commands.Chats.Start;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Chats.Start;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Post("/api/chats");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Start Chat")
                .WithDescription("Creates a new chat and queues the initial message for AI processing.")
                .Produces<Response>(202, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Chats);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        StartChatCommand command = new
        (
            Message: endpointRequest.Message,
            ModelId: endpointRequest.ModelId,
            WebSearchEnabled: endpointRequest.WebSearchEnabled,
            AttachmentDto: endpointRequest.Attachment is not null
                ? new AttachmentDto
                (
                    FileKey: endpointRequest.Attachment.FileKey,
                    ContentType: endpointRequest.Attachment.ContentType,
                    FileSizeInBytes: endpointRequest.Attachment.FileSizeInBytes
                )
                : null
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            mapper: scr => new Response
            (
                ChatId: scr.ChatId,
                StreamId: scr.StreamId,
                ChatTitle: scr.ChatTitle,
                ModelId: scr.ModelId,
                CreatedAt: scr.CreatedAt
            ),
            successStatusCode: 202,
            ct
        );
    }
}