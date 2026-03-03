using FastEndpoints;

using Main.Application.Commands.Workflows.Update;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Workflows.Update;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Put("/api/workflows/{workflowId}");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Update Workflow")
                .WithDescription("Updates an existing workflow's configuration and schedule.")
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
        UpdateWorkflowCommand command = new
        (
            WorkflowId: endpointRequest.WorkflowId,
            Title: endpointRequest.Title,
            Instruction: endpointRequest.Instruction,
            ModelId: endpointRequest.ModelId,
            UseWebSearch: endpointRequest.UseWebSearch,
            RecurrenceKind: endpointRequest.Schedule.Kind,
            DaysOfWeek: endpointRequest.Schedule.DaysOfWeek,
            LocalTime: endpointRequest.Schedule.LocalTime,
            TimeZoneId: endpointRequest.Schedule.TimeZoneId
        );

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            mapper: r => new Response
            (
                WorkflowId: r.WorkflowId,
                Title: r.Title,
                NextRunAt: r.NextRunAt,
                UpdatedAt: r.UpdatedAt
            ),
            cancellationToken: ct
        );
    }
}