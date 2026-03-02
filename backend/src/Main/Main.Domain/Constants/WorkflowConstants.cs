namespace Main.Domain.Constants;

public static class WorkflowConstants
{
    public const int MaxActiveWorkflowsPerUser = 10;

    public const int MinIntervalMinutes = 60;

    public const int MaxInstructionLength = 2000;

    public const int MaxResultTokens = 4000;

    public const int MaxTitleLength = 200;

    public const int MaxModelIdLength = 64;

    public const int MaxTimeZoneIdLength = 64;

    public const int MaxLocalTimeLength = 5; // "HH:mm"

    public const int MaxScheduleSummaryLength = 256;

    public const int MaxConsecutiveFailures = 3;

    public const int ResultPreviewLength = 200;
}