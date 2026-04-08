using FastEndpoints;

using Main.Application.Abstractions.Storage;
using Main.Application.Commands.Chats.SendMessage;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Chats.SendMessage;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Post("/api/chats/{chatId}/messages");
        Version(1);

        Options(o => o.RequireRateLimiting("ai-generation"));

        Description(d =>
        {
            d.WithSummary("Send Message")
                .WithDescription(
                    "Sends a user message to an existing chat and queues AI response generation. " +
                    "The AI response will be streamed via Redis Pub/Sub.")
                .Produces<Response>(202, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Chats);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        SendMessageCommand command = new
        (
            ChatId: endpointRequest.ChatId,
            Message: endpointRequest.Message,
            WebSearchEnabled: endpointRequest.WebSearchEnabled,
            AttachmentDto: endpointRequest.Attachment is { FileKey: not null and not "" }
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
            mapper: response => new Response
            (
                MessageId: response.MessageId,
                ChatId: response.ChatId,
                StreamId: response.StreamId,
                MessageRole: response.MessageRole,
                MessageContent: response.MessageContent,
                CreatedAt: response.CreatedAt
            ),
            successStatusCode: 202,
            cancellationToken: ct
        );
    }
}