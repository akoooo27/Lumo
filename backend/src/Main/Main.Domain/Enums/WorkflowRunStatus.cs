namespace Main.Domain.Enums;

public enum WorkflowRunStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Skipped = 4
}