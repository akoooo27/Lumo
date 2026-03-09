using FastEndpoints;

using Main.Application.Queries.Preferences.GetInstructions;

using Mediator;

using SharedKernel.Api.Constants;
using SharedKernel.Api.Infrastructure;

namespace Main.Api.Endpoints.Preferences.GetInstructions;

internal sealed class Endpoint : EndpointWithoutRequest<Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/preferences/instructions");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Instructions")
                .WithDescription("Retrieves all custom instructions for the authenticated user.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(401, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Preferences);
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        GetInstructionsQuery query = new();

        var outcome = await _sender.Send(query, ct);

        if (outcome.IsFailure)
        {
            await Send.ResultAsync(CustomResults.Problem(outcome, HttpContext));
            return;
        }

        Response response = new
        (
            Instructions: outcome.Value.Instructions
                .Select(i => new InstructionDto
                (
                    Id: i.Id,
                    Content: i.Content,
                    Priority: i.Priority,
                    CreatedAt: i.CreatedAt,
                    UpdatedAt: i.UpdatedAt
                ))
                .ToList()
        );

        await Send.ResponseAsync(response, cancellation: ct);
    }
}