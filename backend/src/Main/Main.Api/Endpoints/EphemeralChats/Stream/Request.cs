using FastEndpoints;

namespace Main.Api.Endpoints.EphemeralChats.Stream;

internal sealed record Request
{
    [RouteParam]
    public required string EphemeralChatId { get; init; }

    [QueryParam]
    public required string StreamId { get; init; }
}