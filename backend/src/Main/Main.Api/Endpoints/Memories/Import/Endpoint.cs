using FastEndpoints;

using Main.Application.Commands.Memories.Import;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Memories.Import;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Post("/api/memories/import");
        Version(1);

        Options(o => o.RequireRateLimiting("ai-generation"));

        Description(d =>
        {
            d.WithSummary("Import Memories")
                .WithDescription(
                    "Imports memories from a structured text export from another AI platform. " +
                    "Deduplicates against existing memories and respects the per-user memory limit.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Memories);
        });
    }

    public override async Task HandleAsync(Request request, CancellationToken ct)
    {
        ImportMemoriesCommand command = new(Content: request.Content);

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            mapper: imr => new Response
            (
                Imported: imr.Imported,
                SkippedAsDuplicates: imr.SkippedAsDuplicates,
                SkippedDueToCapacity: imr.SkippedDueToCapacity,
                Total: imr.Total,
                ImportedAt: imr.ImportedAt
            ),
            cancellationToken: ct
        );
    }
}