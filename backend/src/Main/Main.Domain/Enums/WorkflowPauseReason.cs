namespace Main.Domain.Enums;

public enum WorkflowPauseReason
{
    None = 0,
    UserAction = 1,
    ConsecutiveFailures = 2,
    ModelUnavailable = 3
}