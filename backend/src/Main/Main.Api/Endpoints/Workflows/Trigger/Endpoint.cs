using FastEndpoints;

using Main.Application.Commands.Workflows.Trigger;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Workflows.Trigger;

internal sealed class Endpoint : BaseEndpoint<EmptyRequest, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Post("/api/workflows/{workflowId}/trigger");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Trigger Workflow")
                .WithDescription("Manually triggers a workflow run immediately, bypassing the schedule.")
                .Produces<Response>(202, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(409, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Workflows);
        });
    }

    public override async Task HandleAsync(EmptyRequest _, CancellationToken ct)
    {
        string workflowId = Route<string>("workflowId")!;

        TriggerWorkflowCommand command = new(workflowId);

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            mapper: twr => new Response
            (
                WorkflowRunId: twr.WorkflowRunId,
                ScheduledFor: twr.ScheduledFor,
                CreatedAt: twr.CreatedAt
            ),
            successStatusCode: 202,
            cancellationToken: ct
        );
    }
}