using SharedKernel;

namespace Main.Application.Faults;

public static class WorkflowOperationFaults
{
    public static readonly Fault InvalidLocalTime = Fault.Validation
    (
        title: "Workflow.InvalidLocalTime",
        detail: "Local time must be in HH:mm format."
    );

    public static readonly Fault InvalidTimeZone = Fault.Validation
    (
        title: "Workflow.InvalidTimeZone",
        detail: "The timezone ID is not valid."
    );

    public static readonly Fault MaxWorkflowsReached = Fault.Conflict
    (
        title: "Workflow.MaxWorkflowsReached",
        detail: "You have reached the maximum number of active workflows."
    );

    public static readonly Fault DuplicateWorkflow = Fault.Conflict
    (
        title: "Workflow.DuplicateWorkflow",
        detail: "An active workflow with the same instruction and schedule already exists."
    );

    public static readonly Fault InvalidModel = Fault.Validation
    (
        title: "Workflow.InvalidModel",
        detail: "The selected model is not available for workflows."
    );

    public static readonly Fault NotOwner = Fault.Forbidden
    (
        title: "Workflow.NotOwner",
        detail: "You do not own this workflow."
    );

    public static readonly Fault ModelNoLongerAvailable = Fault.Conflict
    (
        title: "Workflow.ModelNoLongerAvailable",
        detail: "The selected model is no longer available for workflows. Please select a different model."
    );
}