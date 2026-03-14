using FastEndpoints;

using Main.Application.Commands.Folders.Create;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Folders.Create;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Post("/api/folders");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Create Folder")
                .WithDescription("Creates a new folder for organizing chats.")
                .Produces<Response>(201, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(409, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Folders);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        CreateFolderCommand command = new(Name: endpointRequest.Name);

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            mapper: r => new Response
            (
                FolderId: r.FolderId,
                Name: r.Name,
                SortOrder: r.SortOrder,
                CreatedAt: r.CreatedAt
            ),
            successStatusCode: 201,
            cancellationToken: ct
        );
    }
}