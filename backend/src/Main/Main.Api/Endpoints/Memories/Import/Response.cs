namespace Main.Api.Endpoints.Memories.Import;

internal sealed record Response
(
    int Imported,
    int SkippedAsDuplicates,
    int SkippedDueToCapacity,
    int Total,
    DateTimeOffset ImportedAt
);