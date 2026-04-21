using FastEndpoints;

using Main.Application.Commands.EphemeralChats.StopGeneration;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.EphemeralChats.StopGeneration;

internal sealed class Endpoint : BaseEndpoint<Request>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Post("/api/ephemeral-chats/{ephemeralChatId}/stop-generation");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Stop Ephemeral Generation")
                .WithDescription("Stops the AI response generation for the specified ephemeral chat. The partial response is discarded.")
                .Produces(204)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(409, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.EphemeralChats);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        StopEphemeralGenerationCommand command = new(endpointRequest.EphemeralChatId, endpointRequest.StreamId);

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            cancellationToken: ct
        );
    }
}