using FastEndpoints;

using Main.Application.Queries.Folders.GetFolders;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Folders.GetFolders;

internal sealed class Endpoint : BaseEndpoint<EmptyRequest, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/folders");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Folders")
                .WithDescription("Retrieves all folders for the authenticated user with chat counts.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Folders);
        });
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        GetFoldersQuery query = new();

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(query, ct),
            mapper: response => new Response
            (
                Folders: response.Folders
                    .Select(f => new FolderDto
                    (
                        FolderId: f.FolderId,
                        Name: f.Name,
                        SortOrder: f.SortOrder,
                        ChatCount: f.ChatCount
                    ))
                    .ToList()
            ),
            cancellationToken: ct
        );
    }
}