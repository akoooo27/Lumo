using FastEndpoints;

using Main.Application.Commands.Folders.Delete;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Folders.Delete;

internal sealed class Endpoint : BaseEndpoint<EmptyRequest>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Delete("/api/folders/{folderId}");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Delete Folder")
                .WithDescription("Deletes a folder and unassigns all chats from it.")
                .Produces(204)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Folders);
        });
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        string folderId = Route<string>("folderId")!;

        DeleteFolderCommand command = new(FolderId: folderId);

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            cancellationToken: ct
        );
    }
}