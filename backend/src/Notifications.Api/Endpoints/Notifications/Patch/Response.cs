namespace Notifications.Api.Endpoints.Notifications.Patch;

internal sealed record Response
(
    Guid Id,
    string Status,
    DateTimeOffset? ReadAt
);