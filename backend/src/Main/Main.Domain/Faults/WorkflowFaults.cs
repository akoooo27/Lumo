using SharedKernel;

namespace Main.Domain.Faults;

public static class WorkflowFaults
{
    public static readonly Fault UserIdRequired = Fault.Validation
    (
        title: "Workflow.UserIdRequired",
        detail: "A user ID is required."
    );

    public static readonly Fault TitleRequired = Fault.Validation
    (
        title: "Workflow.TitleRequired",
        detail: "A title is required."
    );

    public static readonly Fault TitleTooLong = Fault.Validation
    (
        title: "Workflow.TitleTooLong",
        detail: "The title exceeds the maximum allowed length."
    );

    public static readonly Fault InstructionRequired = Fault.Validation
    (
        title: "Workflow.InstructionRequired",
        detail: "An instruction is required."
    );

    public static readonly Fault InstructionTooLong = Fault.Validation
    (
        title: "Workflow.InstructionTooLong",
        detail: $"The instruction exceeds the maximum allowed length of {Constants.WorkflowConstants.MaxInstructionLength} characters."
    );

    public static readonly Fault ModelIdRequired = Fault.Validation
    (
        title: "Workflow.ModelIdRequired",
        detail: "A model ID is required."
    );

    public static readonly Fault InvalidRecurrenceKind = Fault.Validation
    (
        title: "Workflow.InvalidRecurrenceKind",
        detail: "The recurrence kind is invalid."
    );

    public static readonly Fault WeeklyRequiresDays = Fault.Validation
    (
        title: "Workflow.WeeklyRequiresDays",
        detail: "Weekly recurrence requires at least one weekday."
    );

    public static readonly Fault CannotModifyArchived = Fault.Conflict
    (
        title: "Workflow.CannotModifyArchived",
        detail: "Cannot modify an archived workflow."
    );

    public static readonly Fault AlreadyPaused = Fault.Conflict
    (
        title: "Workflow.AlreadyPaused",
        detail: "The workflow is already paused."
    );

    public static readonly Fault NotPaused = Fault.Conflict
    (
        title: "Workflow.NotPaused",
        detail: "The workflow is not paused."
    );

    public static readonly Fault NotActive = Fault.Conflict
    (
        title: "Workflow.NotActive",
        detail: "The workflow is not active."
    );

    public static readonly Fault NotFound = Fault.NotFound
    (
        title: "Workflow.NotFound",
        detail: "The specified workflow was not found."
    );
}