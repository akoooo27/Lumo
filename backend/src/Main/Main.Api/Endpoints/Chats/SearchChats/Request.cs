using Main.Domain.Constants;

using Microsoft.AspNetCore.Mvc;

namespace Main.Api.Endpoints.Chats.SearchChats;

internal sealed record Request
{
    [FromQuery(Name = "q")]
    public string Query { get; init; } = string.Empty;

    [FromQuery]
    public DateTimeOffset? Cursor { get; init; }

    [FromQuery]
    public int Limit { get; init; } = ChatConstants.SearchDefaultPageSize;
}