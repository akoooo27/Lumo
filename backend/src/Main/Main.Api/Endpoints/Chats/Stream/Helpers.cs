using System.Text.Json;

using Main.Application.Abstractions.Stream;

namespace Main.Api.Endpoints.Chats.Stream;

internal static class Helpers
{
    public static string FormatStreamMessage(StreamMessage message)
    {
        return message.Type switch
        {
            StreamMessageType.Chunk => FormatTextChunk(message.Content),
            StreamMessageType.ToolCall => FormatToolCallMessage(message.Content, message.Query),
            StreamMessageType.ToolCallResult => FormatToolCallResultMessage(message.Content, message.Sources),
            StreamMessageType.Status when message.Content == "done" => FormatFinishMessage(message.ModelName, message.Provider),
            StreamMessageType.Status when message.Content == "failed" => FormatErrorMessage("AI Generation Failed"),
            _ => string.Empty
        };
    }

    private static string FormatTextChunk(string text)
    {
        string escaped = JsonSerializer.Serialize(text);
        return $"0:{escaped}\n";
    }

    private static string FormatToolCallMessage(string toolName, string? query)
    {
        string json = query is not null
            ? JsonSerializer.Serialize(new { type = "tool_call", tool = toolName, query })
            : JsonSerializer.Serialize(new { type = "tool_call", tool = toolName });

        return $"2:[{json}]\n";
    }

    private static string FormatToolCallResultMessage(string toolName, string? sourcesJson)
    {
        if (sourcesJson is null)
            return string.Empty;

        string json = $"{{\"type\":\"tool_result\",\"tool\":{JsonSerializer.Serialize(toolName)},\"sources\":{sourcesJson}}}";
        return $"2:[{json}]\n";
    }

    private static string FormatFinishMessage(string? modelName, string? provider)
    {
        var payload = new Dictionary<string, string> { ["finishReason"] = "stop" };

        if (modelName is not null)
            payload["model"] = modelName;

        if (provider is not null)
            payload["provider"] = provider;

        return $"d:{JsonSerializer.Serialize(payload)}\n";
    }

    private static string FormatErrorMessage(string error)
    {
        string escaped = JsonSerializer.Serialize(error);
        return $"3:{escaped}\n";
    }
}