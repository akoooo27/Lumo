using FastEndpoints;

using Main.Application.Queries.Workflows.GetWorkflowRuns;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Workflows.GetWorkflowRuns;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/workflows/{workflowId}/runs");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Workflow Runs")
                .WithDescription("Retrieves the execution history for a specific workflow.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(403, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Workflows);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        GetWorkflowRunsQuery query = new(endpointRequest.WorkflowId);

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(query, ct),
            mapper: r => new Response
            (
                WorkflowRuns: r.WorkflowRuns
                    .Select(run => new WorkflowRunListItemDto
                    (
                        WorkflowRunId: run.WorkflowRunId,
                        Status: run.Status,
                        ScheduledFor: run.ScheduledFor,
                        StartedAt: run.StartedAt,
                        CompletedAt: run.CompletedAt,
                        FailureMessage: run.FailureMessage,
                        SkipReason: run.SkipReason,
                        CreatedAt: run.CreatedAt
                    ))
                    .ToList()
            ),
            cancellationToken: ct
        );
    }
}