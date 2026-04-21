using FastEndpoints;

using Main.Application.Commands.Chats.StopGeneration;

using Mediator;

using SharedKernel.Api.Constants;

namespace Main.Api.Endpoints.Chats.StopGeneration;

internal sealed class Endpoint : BaseEndpoint<Request>
{
    private readonly ISender _sender;

    public Endpoint(ISender sender)
    {
        _sender = sender;
    }

    public override void Configure()
    {
        Post("/api/chats/{chatId}/stop-generation");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Stop Generation")
                .WithDescription("Stops the AI response generation for the specified chat. The partial response is discarded.")
                .Produces(204)
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(409, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Chats);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        StopGenerationCommand command = new(endpointRequest.ChatId, endpointRequest.StreamId);

        await SendOutcomeAsync
        (
            outcome: await _sender.Send(command, ct),
            cancellationToken: ct
        );
    }
}