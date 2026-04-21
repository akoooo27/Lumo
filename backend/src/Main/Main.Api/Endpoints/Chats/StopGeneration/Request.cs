namespace Main.Api.Endpoints.Chats.StopGeneration;

internal sealed record Request(string ChatId, string StreamId);