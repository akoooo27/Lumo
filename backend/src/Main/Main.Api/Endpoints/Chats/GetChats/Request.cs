using Main.Domain.Constants;

using Microsoft.AspNetCore.Mvc;

namespace Main.Api.Endpoints.Chats.GetChats;

internal sealed record Request
{
    [FromQuery]
    public DateTimeOffset? Cursor { get; init; }

    [FromQuery]
    public int Limit { get; init; } = ChatConstants.DefaultPageSize;

    [FromQuery]
    public string? FolderId { get; init; }
}