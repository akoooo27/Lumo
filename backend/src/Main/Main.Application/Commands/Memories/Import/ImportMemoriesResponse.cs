namespace Main.Application.Commands.Memories.Import;

public sealed record ImportMemoriesResponse
(
    int Imported,
    int SkippedAsDuplicates,
    int SkippedDueToCapacity,
    int Total,
    DateTimeOffset ImportedAt
);