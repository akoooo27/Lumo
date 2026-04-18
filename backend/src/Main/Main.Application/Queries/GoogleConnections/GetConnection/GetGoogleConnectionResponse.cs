namespace Main.Application.Queries.GoogleConnections.GetConnection;

public sealed record GetGoogleConnectionResponse(bool IsConnected, string? GoogleEmail);