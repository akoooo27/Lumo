namespace Main.Application.Abstractions.Memory;

public sealed record ImportEntry
(
    string Content,
    MemoryCategory Category,
    int Importance
);