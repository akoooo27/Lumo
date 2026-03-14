using System.Text;

using FastEndpoints;

using Main.Application.Abstractions.Services;
using Main.Application.Abstractions.Stream;
using Main.Domain.ValueObjects;

using SharedKernel;
using SharedKernel.Api.Constants;
using SharedKernel.Api.Infrastructure;

namespace Main.Api.Endpoints.Chats.Stream;

internal sealed class Endpoint : Endpoint<Request>
{
    private readonly IChatAccessValidator _chatAccessValidator;
    private readonly IStreamReader _streamReader;

    public Endpoint(IChatAccessValidator chatAccessValidator, IStreamReader streamReader)
    {
        _chatAccessValidator = chatAccessValidator;
        _streamReader = streamReader;
    }

    public override void Configure()
    {
        Get("/api/chats/{chatId}/stream");
        Version(1);

        Description(d =>
        {
            d.WithSummary("Stream Chat Response")
                .WithDescription(
                    "Server-Sent Events endpoint for streaming AI responses. " +
                    "Requires streamId query parameter from SendMessage response. " +
                    "Compatible with Vercel AI SDK Data Stream Protocol.")
                .Produces(200, contentType: "text/event-stream")
                .ProducesProblemDetails(400, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(401, HttpContentTypeConstants.Json)
                .ProducesProblemDetails(404, HttpContentTypeConstants.Json)
                .WithTags(CustomTags.Chats);
        });
    }

    public override async Task HandleAsync(Request endpointRequest, CancellationToken ct)
    {
        string chatId = endpointRequest.ChatId;
        string streamId = endpointRequest.StreamId;

        Outcome<StreamId> streamIdOutcome = StreamId.From(streamId);

        if (streamIdOutcome.IsFailure)
        {
            await Send.ResponseAsync(CustomResults.Problem(streamIdOutcome, HttpContext), cancellation: ct);
            return;
        }

        StreamId safeStreamId = streamIdOutcome.Value;

        Outcome accessOutcome = await _chatAccessValidator.ValidateAccessAsync(chatId, ct);

        if (accessOutcome.IsFailure)
        {
            await Send.ResponseAsync(CustomResults.Problem(accessOutcome, HttpContext), cancellation: ct);
            return;
        }

        HttpContext.Response.Headers.ContentType = "text/event-stream";
        HttpContext.Response.Headers.CacheControl = "no-cache";
        HttpContext.Response.Headers.Connection = "keep-alive";

        HttpContext.Response.Headers["X-Vercel-AI-Data-Stream"] = "v1";

        await HttpContext.Response.Body.FlushAsync(ct);

        try
        {
            await foreach (StreamMessage message in _streamReader.ReadStreamAsync(safeStreamId.Value, ct))
            {
                string sseData = Helpers.FormatStreamMessage(message);

                if (!string.IsNullOrEmpty(sseData))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(sseData);
                    await HttpContext.Response.Body.WriteAsync(bytes, ct);
                    await HttpContext.Response.Body.FlushAsync(ct);
                }

                if (message is { Type: StreamMessageType.Status, Content: "done" or "failed" })
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - normal behavior
        }
    }
}