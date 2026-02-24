using Main.Application.Abstractions.AI;

using SharedKernel;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Models;

internal sealed class GetAvailableModelsHandler(IModelRegistry modelRegistry) : IQueryHandler<GetAvailableModelsQuery, GetAvailableModelsResponse>
{
    public ValueTask<Outcome<GetAvailableModelsResponse>> Handle(GetAvailableModelsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ModelInfo> models = modelRegistry.GetAvailableModels();

        List<AvailableModelDto> dtos = models.Select(m => new AvailableModelDto
        (
            Id: m.Id,
            DisplayName: m.DisplayName,
            Provider: m.Provider,
            IsDefault: m.IsDefault,
            MaxContextTokens: m.ModelCapabilities.MaxContextTokens,
            SupportsVision: m.ModelCapabilities.SupportsVision,
            SupportsFunctionCalling: m.ModelCapabilities.SupportsFunctionCalling
        )).ToList();

        GetAvailableModelsResponse response = new(dtos);

        return ValueTask.FromResult(Outcome.Success(response));
    }
}