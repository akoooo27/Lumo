using FastEndpoints;

using Main.Application.Commands.Workflows.Patch;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Workflows.Patch;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Patch("/api/workflows/{workflowId}");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Patch Workflow")
                .WithDescription("Partially updates a workflow. Supports pausing, resuming, and archiving.")
                .Produces<Response>(200, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(403, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(409, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Workflows);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        PatchWorkflowCommand command = new
        (
            WorkflowId: endpointRequest.WorkflowId,
            Status: endpointRequest.Status
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            mapper: r => new Response
            (
                WorkflowId: r.WorkflowId,
                Status: r.Status,
                PauseReason: r.PauseReason,
                NextRunAt: r.NextRunAt,
                UpdatedAt: r.UpdatedAt
            ),
            cancellationToken: ct
        );
    }
}