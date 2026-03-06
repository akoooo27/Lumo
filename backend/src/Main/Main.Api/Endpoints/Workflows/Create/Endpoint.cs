using FastEndpoints;

using Main.Application.Commands.Workflows.Create;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Workflows.Create;

internal sealed class Endpoint : BaseEndpoint<Request, Response>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Post("/api/workflows");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Create Workflow")
                .WithDescription("Creates a new recurring workflow.")
                .Produces<Response>(201, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(409, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Workflows);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        CreateWorkflowCommand command = new
        (
            Title: endpointRequest.Title,
            Instruction: endpointRequest.Instruction,
            ModelId: endpointRequest.ModelId,
            UseWebSearch: endpointRequest.UseWebSearch,
            RecurrenceKind: endpointRequest.Schedule.Kind,
            DayOfWeeks: endpointRequest.Schedule.DaysOfWeek,
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
                CreatedAt: r.CreatedAt
            ),
            successStatusCode: 201,
            cancellationToken: ct
        );
    }
}