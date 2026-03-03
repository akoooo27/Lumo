using FastEndpoints;

using Main.Application.Queries.Workflows.GetWorkflow;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Workflows.GetWorkflow;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Get("/api/workflows/{workflowId}");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Get Workflow")
                .WithDescription("Retrieves the details of a specific workflow.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(403, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Workflows);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        GetWorkflowQuery query = new(endpointRequest.WorkflowId);

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(query, ct),
            mapper: r => new Response
            (
                WorkflowId: r.WorkflowId,
                Title: r.Title,
                Instruction: r.Instruction,
                ModelId: r.ModelId,
                UseWebSearch: r.UseWebSearch,
                Status: r.Status,
                PauseReason: r.PauseReason,
                RecurrenceKind: r.RecurrenceKind,
                DaysOfWeek: r.DaysOfWeek,
                LocalTime: r.LocalTime,
                TimeZoneId: r.TimeZoneId,
                NextRunAt: r.NextRunAt,
                LastRunAt: r.LastRunAt,
                ConsecutiveFailureCount: r.ConsecutiveFailureCount,
                CreatedAt: r.CreatedAt,
                UpdatedAt: r.UpdatedAt
            ),
            cancellationToken: ct
        );
    }
}