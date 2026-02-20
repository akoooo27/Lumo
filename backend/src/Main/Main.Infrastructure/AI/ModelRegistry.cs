using Main.Application.Abstractions.AI;
using Main.Infrastructure.Options;

using Microsoft.Extensions.Options;

namespace Main.Infrastructure.AI;

internal sealed class ModelRegistry : IModelRegistry
{
    private readonly IReadOnlyList<ModelInfo> _models;
    private readonly Dictionary<string, ModelConfiguration> _modelMap;
    private readonly string _defaultModelId;

    public ModelRegistry(IOptions<OpenRouterOptions> openRouterOptions)
    {
        OpenRouterOptions options = openRouterOptions.Value;

        _models = options.AllowedModels
            .Select(m => new ModelInfo
            (
                Id: m.Id,
                DisplayName: m.DisplayName,
                Provider: m.Provider,
                IsDefault: m.IsDefault,
                ModelCapabilities: new ModelCapabilities
                (
                    MaxContextTokens: m.MaxContextTokens,
                    SupportsVision: m.SupportsVision,
                    SupportsStreaming: m.SupportsStreaming,
                    SupportsFunctionCalling: m.SupportsFunctionCalling
                )
                )).ToList();

        _modelMap = options.AllowedModels
            .ToDictionary(m => m.Id, m => m);

        _defaultModelId = options.AllowedModels
                              .FirstOrDefault(m => m.IsDefault)?.Id
                          ?? options.DefaultModel;
    }

    public IReadOnlyList<ModelInfo> GetAvailableModels() => _models;

    public bool IsModelAllowed(string modelId) =>
        _modelMap.ContainsKey(modelId);

    public string GetOpenRouterModelId(string modelId) =>
        _modelMap.TryGetValue(modelId, out var config)
            ? config.OpenRouterId
            : throw new ArgumentException($"Model '{modelId}' is not in the allowed models list. Call IsModelAllowed() first.", nameof(modelId));

    public string GetDefaultModelId() =>
        _defaultModelId;

    public ModelInfo? GetModelInfo(string modelId) =>
        _models.FirstOrDefault(m => m.Id == modelId);
}