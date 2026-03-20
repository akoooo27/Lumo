using FastEndpoints;

using Main.Application.Commands.Chats.GetAttachmentUploadUrl;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Chats.GetAttachmentUploadUrl;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Post("/api/chats/attachments/upload-url");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Attachment Upload URL")
                .WithDescription("Generates a presigned URL for uploading a chat attachment to S3.")
                .Produces<Response>(201, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Chats);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        GetAttachmentUploadUrlCommand command = new
        (
            ContentType: endpointRequest.ContentType,
            ContentLength: endpointRequest.ContentLength
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            mapper: r => new Response
            (
                UploadUrl: r.UploadUrl,
                FileKey: r.FileKey,
                ExpiresAt: r.ExpiresAt
            ),
            successStatusCode: 201,
            cancellationToken: ct
        );
    }
}