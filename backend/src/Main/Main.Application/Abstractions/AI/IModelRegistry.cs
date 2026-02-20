namespace Main.Application.Abstractions.AI;

public interface IModelRegistry
{
    IReadOnlyList<ModelInfo> GetAvailableModels();

    bool IsModelAllowed(string modelId);

    string GetOpenRouterModelId(string modelId);

    string GetDefaultModelId();

    ModelInfo? GetModelInfo(string modelId);
}

public sealed record ModelInfo
(
    string Id,
    string DisplayName,
    string Provider,
    bool IsDefault,
    ModelCapabilities ModelCapabilities
);

public sealed record ModelCapabilities
(
    int MaxContextTokens,
    bool SupportsVision,
    bool SupportsStreaming,
    bool SupportsFunctionCalling
);