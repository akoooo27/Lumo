namespace Main.Application.Abstractions.Stream;

public enum StreamMessageType
{
    Chunk = 0,
    Status = 1,
    ToolCall = 2,
    ToolCallResult = 3,
    Thinking = 4,
    Memories = 5
}