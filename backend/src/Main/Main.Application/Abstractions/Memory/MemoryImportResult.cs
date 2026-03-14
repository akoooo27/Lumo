namespace Main.Application.Abstractions.Memory;

public sealed record MemoryImportResult
(
    int Imported,
    int SkippedAsDuplicates,
    int SkippedDueToCapacity,
    int Total
);