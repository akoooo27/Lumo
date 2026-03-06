using System.Globalization;

using Cronos;

using Main.Application.Abstractions.Workflows;
using Main.Application.Faults;
using Main.Domain.Enums;

using SharedKernel;

namespace Main.Infrastructure.Workflows;

internal sealed class WorkflowScheduleService : IWorkflowScheduleService
{
    public Outcome ValidateScheduleInputs(string localTime, string timeZoneId)
    {
        if (!TimeOnly.TryParseExact(localTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return WorkflowOperationFaults.InvalidLocalTime;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return WorkflowOperationFaults.InvalidTimeZone;
        }

        return Outcome.Success();
    }

    public DateTimeOffset GetNextOccurrence(WorkflowRecurrenceKind kind, IReadOnlyList<DayOfWeek>? daysOfWeek, string localTime,
        string timeZoneId, DateTimeOffset fromUtc)
    {
        string cron = BuildCronExpression(kind, daysOfWeek, localTime);
        CronExpression cronExpression = CronExpression.Parse(cron);
        TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        DateTime? next = cronExpression.GetNextOccurrence(fromUtc.UtcDateTime, timeZoneInfo);

        if (next is null)
            throw new InvalidOperationException("Could not compute next occurrence for the given schedule.");

        return new DateTimeOffset(next.Value, TimeSpan.Zero);
    }

    private static string BuildCronExpression
    (
        WorkflowRecurrenceKind kind,
        IReadOnlyList<DayOfWeek>? daysOfWeek,
        string localTime
    )
    {
        TimeOnly time = TimeOnly.ParseExact(localTime, "HH:mm", CultureInfo.InvariantCulture);

        string dayOfWeekField = kind switch
        {
            WorkflowRecurrenceKind.Daily => "*",
            WorkflowRecurrenceKind.Weekdays => "1-5",
            WorkflowRecurrenceKind.Weekly when daysOfWeek is { Count: > 0 } =>
                string.Join(',', daysOfWeek.Select(d => (int)d)),
            _ => "*"
        };

        // minute hour * * dayOfWeek
        return $"{time.Minute} {time.Hour} * * {dayOfWeekField}";
    }
}