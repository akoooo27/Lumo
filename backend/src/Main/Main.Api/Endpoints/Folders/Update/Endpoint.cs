using FastEndpoints;

using Main.Application.Commands.Folders.Update;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Folders.Update;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Patch("/api/folders/{folderId}");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Update Folder")
                .WithDescription("Partially updates a folder. Supports renaming and reordering.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(409, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Folders);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        UpdateFolderCommand command = new
        (
            FolderId: endpointRequest.FolderId,
            NewName: endpointRequest.NewName,
            SortOrder: endpointRequest.SortOrder
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            mapper: r => new Response
            (
                FolderId: r.FolderId,
                Name: r.Name,
                SortOrder: r.SortOrder,
                UpdatedAt: r.UpdatedAt
            ),
            cancellationToken: ct
        );
    }
}