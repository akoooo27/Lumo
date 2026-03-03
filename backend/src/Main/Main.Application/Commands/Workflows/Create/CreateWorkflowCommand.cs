using Main.Domain.Enums;

using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Workflows.Create;

public sealed record CreateWorkflowCommand
(
    string? Title,
    string Instruction,
    string ModelId,
    bool UseWebSearch,
    WorkflowRecurrenceKind RecurrenceKind,
    IReadOnlyList<DayOfWeek>? DayOfWeeks,
    string LocalTime,
    string TimeZoneId
) : ICommand<CreateWorkflowResponse>;