namespace Main.Api.Endpoints.GoogleConnection.GetConnection;

internal sealed record Response(bool IsConnected, string? GoogleEmail);