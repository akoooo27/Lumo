using FastEndpoints;

using Main.Application.Queries.Models;

using Mediator;

using SharedKernel.Api.Constants;
using SharedKernel.Api.Infrastructure;

namespace Main.Api.Endpoints.Models.GetModels;

internal sealed class Endpoint : EndpointWithoutRequest<Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/models");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Available Models")
                .WithDescription(
                    "Retrieves all available AI models that can be used for chat. " +
                    "Returns model metadata including capabilities and provider information.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Models);
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        GetAvailableModelsQuery query = new();

        var outcome = await _sender.Send(query, ct);

        if (outcome.IsFailure)
        {
            await Send.ResultAsync(CustomResults.Problem(outcome, HttpContext));
            return;
        }

        Response response = new
        (
            Models: outcome.Value.Models
                .Select(m => new ModelDto
                (
                    Id: m.Id,
                    DisplayName: m.DisplayName,
                    Provider: m.Provider,
                    IsDefault: m.IsDefault,
                    MaxContextTokens: m.MaxContextTokens,
                    SupportsVision: m.SupportsVision,
                    SupportsFunctionCalling: m.SupportsFunctionCalling
                ))
                .ToList()
        );

        await Send.ResponseAsync(response, cancellation: ct);
    }
}