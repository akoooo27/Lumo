using Main.Application.Abstractions.AI;
using Main.Domain.Enums;

using SharedKernel;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Workflows.GetWorkflowOptions;

internal sealed class GetWorkflowOptionsHandler(IModelRegistry modelRegistry)
    : IQueryHandler<GetWorkflowOptionsQuery, GetWorkflowOptionsResponse>
{
    public ValueTask<Outcome<GetWorkflowOptionsResponse>> Handle(GetWorkflowOptionsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkflowModelOptionReadModel> models = modelRegistry.GetAvailableModels()
            .Select(m => new WorkflowModelOptionReadModel
            {
                Id = m.Id,
                DisplayName = m.DisplayName,
                Provider = m.Provider,
                IsDefault = m.IsDefault,
                SupportsFunctionCalling = m.ModelCapabilities.SupportsFunctionCalling
            })
            .ToList();

        IReadOnlyList<WorkflowRecurrenceKind> recurrenceKinds = Enum.GetValues<WorkflowRecurrenceKind>();
        IReadOnlyList<DayOfWeek> daysOfWeek = Enum.GetValues<DayOfWeek>();
        IReadOnlyList<string> timeZoneIds = TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => tz.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        GetWorkflowOptionsResponse response = new
        (
            Models: models,
            RecurrenceKinds: recurrenceKinds,
            DaysOfWeek: daysOfWeek,
            TimeZoneIds: timeZoneIds
        );

        return ValueTask.FromResult(Outcome.Success(response));
    }
}
