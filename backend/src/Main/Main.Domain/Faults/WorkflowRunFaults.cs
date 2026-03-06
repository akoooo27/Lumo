using SharedKernel;

namespace Main.Domain.Faults;

public static class WorkflowRunFaults
{
    public static readonly Fault NotFound = Fault.NotFound
    (
        title: "WorkflowRun.NotFound",
        detail: "The specified workflow run was not found."
    );

    public static readonly Fault WorkflowRunIdRequired = Fault.Validation
    (
        title: "WorkflowRun.WorkflowRunIdRequired",
        detail: "A workflow run ID is required."
    );

    public static readonly Fault WorkflowIdRequired = Fault.Validation
    (
        title: "WorkflowRun.WorkflowIdRequired",
        detail: "A workflow ID is required."
    );

    public static readonly Fault ModelIdRequired = Fault.Validation
    (
        title: "WorkflowRun.ModelIdRequired",
        detail: "A model ID is required."
    );

    public static readonly Fault InstructionSnapshotRequired = Fault.Validation
    (
        title: "WorkflowRun.InstructionSnapshotRequired",
        detail: "An instruction snapshot is required."
    );

    public static readonly Fault TitleSnapshotRequired = Fault.Validation
    (
        title: "WorkflowRun.TitleSnapshotRequired",
        detail: "A title snapshot is required."
    );

    public static readonly Fault SkipReasonRequired = Fault.Validation
    (
        title: "WorkflowRun.SkipReasonRequired",
        detail: "A skip reason is required."
    );

    public static readonly Fault NotQueued = Fault.Conflict
    (
        title: "WorkflowRun.NotQueued",
        detail: "The workflow run is not in a queued state."
    );

    public static readonly Fault NotRunning = Fault.Conflict
    (
        title: "WorkflowRun.NotRunning",
        detail: "The workflow run is not in a running state."
    );

    public static readonly Fault CannotFail = Fault.Conflict
    (
        title: "WorkflowRun.CannotFail",
        detail: "The workflow run cannot be marked as failed from its current state."
    );

    public static readonly Fault ResultRequired = Fault.Validation
    (
        title: "WorkflowRun.ResultRequired",
        detail: "A result is required to mark the run as succeeded."
    );

    public static readonly Fault FailureMessageRequired = Fault.Validation
    (
        title: "WorkflowRun.FailureMessageRequired",
        detail: "A failure message is required."
    );
}