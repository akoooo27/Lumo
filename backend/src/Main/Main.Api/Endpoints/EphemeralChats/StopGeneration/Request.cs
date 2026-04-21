namespace Main.Api.Endpoints.EphemeralChats.StopGeneration;

internal sealed record Request(string EphemeralChatId, string StreamId);