using Main.Domain.Enums;

using SharedKernel;

namespace Main.Application.Abstractions.Workflows;

public interface IWorkflowScheduleService
{
    Outcome ValidateScheduleInputs(string localTime, string timeZoneId);

    DateTimeOffset GetNextOccurrence
    (
        WorkflowRecurrenceKind kind,
        IReadOnlyList<DayOfWeek>? daysOfWeek,
        string localTime,
        string timeZoneId,
        DateTimeOffset fromUtc
    );
}