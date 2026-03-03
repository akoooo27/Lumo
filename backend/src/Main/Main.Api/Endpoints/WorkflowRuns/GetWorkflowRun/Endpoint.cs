using FastEndpoints;

using Main.Application.Queries.Workflows.GetWorkflowRun;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.WorkflowRuns.GetWorkflowRun;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/workflow-runs/{runId}");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Workflow Run")
                .WithDescription("Retrieves the full details of a specific workflow run, including the result markdown.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(403, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Workflows);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        GetWorkflowRunQuery query = new(endpointRequest.RunId);

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(query, ct),
            mapper: r => new Response
            (
                WorkflowRunId: r.WorkflowRunId,
                WorkflowId: r.WorkflowId,
                Status: r.Status,
                ScheduledFor: r.ScheduledFor,
                StartedAt: r.StartedAt,
                CompletedAt: r.CompletedAt,
                ResultMarkdown: r.ResultMarkdown,
                ResultPreview: r.ResultPreview,
                FailureMessage: r.FailureMessage,
                SkipReason: r.SkipReason,
                ModelIdUsed: r.ModelIdUsed,
                UseWebSearchUsed: r.UseWebSearchUsed,
                InstructionSnapshot: r.InstructionSnapshot,
                TitleSnapshot: r.TitleSnapshot,
                ScheduleSummarySnapshot: r.ScheduleSummarySnapshot,
                CreatedAt: r.CreatedAt
            ),
            cancellationToken: ct
        );
    }
}