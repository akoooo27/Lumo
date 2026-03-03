using FastEndpoints;

using Main.Application.Queries.Workflows.GetWorkflowOptions;

using Mediator;

using SharedKernel.Api.Constants;
using SharedKernel.Api.Infrastructure;

namespace Main.Api.Endpoints.Workflows.GetOptions;

internal sealed class Endpoint : EndpointWithoutRequest<Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/workflows/options");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Workflow Options")
                .WithDescription("Retrieves available options for creating or updating a workflow, including models, recurrence kinds, days, and timezones.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Workflows);
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        GetWorkflowOptionsQuery query = new();

        var outcome = await _sender.Send(query, ct);

        if (outcome.IsFailure)
        {
            await Send.ResultAsync(CustomResults.Problem(outcome, HttpContext));
            return;
        }

        Response response = new
        (
            Models: outcome.Value.Models
                .Select(m => new WorkflowModelOptionDto
                (
                    Id: m.Id,
                    DisplayName: m.DisplayName,
                    Provider: m.Provider,
                    IsDefault: m.IsDefault,
                    SupportsFunctionCalling: m.SupportsFunctionCalling
                ))
                .ToList(),
            RecurrenceKinds: outcome.Value.RecurrenceKinds,
            DaysOfWeek: outcome.Value.DaysOfWeek,
            TimeZoneIds: outcome.Value.TimeZoneIds
        );

        await Send.ResponseAsync(response, cancellation: ct);
    }
}