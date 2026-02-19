namespace Main.Application.Abstractions.Memory;

public static class MemoryConstants
{
    public const int MaxContentLength = 2000;
    public const int MaxMemoriesPerUser = 100;
    public const int MaxMemoriesInContext = 10;

    public const int MinImportance = 1;
    public const int MaxImportance = 10;
    public const int DefaultImportance = 5;

    public const int MaxMemorySearchResults = 5;

    // Consolidation thresholds
    public const int ConsolidationThreshold = 90;
    public const double DuplicateThreshold = 0.92;
    public const double RelatedThreshold = 0.80;
    public const int StaleDaysThreshold = 90;

    // Pagination
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
}