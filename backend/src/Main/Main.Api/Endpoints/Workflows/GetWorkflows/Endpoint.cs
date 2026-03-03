using FastEndpoints;

using Main.Application.Queries.Workflows.GetWorkflows;

using Mediator;

using SharedKernel.Api.Constants;
using SharedKernel.Api.Infrastructure;

namespace Main.Api.Endpoints.Workflows.GetWorkflows;

internal sealed class Endpoint : EndpointWithoutRequest<Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/workflows");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Workflows")
                .WithDescription("Retrieves all workflows for the current user, excluding archived ones.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Workflows);
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        GetWorkflowsQuery query = new();

        var outcome = await _sender.Send(query, ct);

        if (outcome.IsFailure)
        {
            await Send.ResultAsync(CustomResults.Problem(outcome, HttpContext));
            return;
        }

        Response response = new
        (
            Workflows: outcome.Value.Workflows
                .Select(w => new WorkflowListItemDto
                (
                    WorkflowId: w.WorkflowId,
                    Title: w.Title,
                    Status: w.Status,
                    PauseReason: w.PauseReason,
                    ModelId: w.ModelId,
                    UseWebSearch: w.UseWebSearch,
                    ScheduleSummary: w.ScheduleSummary,
                    NextRunAt: w.NextRunAt,
                    LastRunAt: w.LastRunAt
                ))
                .ToList()
        );

        await Send.ResponseAsync(response, cancellation: ct);
    }
}