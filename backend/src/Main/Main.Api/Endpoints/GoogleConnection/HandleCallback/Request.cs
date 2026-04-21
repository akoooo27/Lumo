using Microsoft.AspNetCore.Mvc;

namespace Main.Api.Endpoints.GoogleConnection.HandleCallback;

internal sealed record Request
{
    [FromQuery]
    public string Code { get; init; } = string.Empty;

    [FromQuery]
    public string State { get; init; } = string.Empty;
}