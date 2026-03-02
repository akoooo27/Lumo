using Main.Domain.Enums;

using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Workflows.Update;

public sealed record UpdateWorkflowCommand
(
    string WorkflowId,
    string? Title,
    string Instruction,
    string ModelId,
    bool UseWebSearch,
    WorkflowRecurrenceKind RecurrenceKind,
    IReadOnlyList<DayOfWeek>? DaysOfWeek,
    string LocalTime,
    string TimeZoneId
) : ICommand<UpdateWorkflowResponse>;
