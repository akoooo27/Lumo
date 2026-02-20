namespace Main.Application.Queries.Models;

public sealed record AvailableModelDto
(
    string Id,
    string DisplayName,
    string Provider,
    bool IsDefault,
    int MaxContextTokens,
    bool SupportsVision,
    bool SupportsFunctionCalling
);