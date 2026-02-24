using SharedKernel;

namespace Main.Domain.Faults;

public static class PreferenceFaults
{
    public static readonly Fault UserIdRequired = Fault.Validation
    (
        title: "Preference.UserIdRequired",
        detail: "A user ID is required to create a preference."
    );

    public static readonly Fault MaxInstructionsReached = Fault.Validation
    (
        title: "Preference.MaxInstructionsReached",
        detail: "The maximum number of instructions has been reached."
    );

    public static readonly Fault InstructionNotFound = Fault.NotFound
    (
        title: "Preference.InstructionNotFound",
        detail: "The specified instruction was not found in the preference."
    );

    public static readonly Fault AlreadyInFavorites = Fault.Validation
    (
        title: "Preference.AlreadyInFavorites",
        detail: "The model is already in the user's favorites."
    );

    public static readonly Fault ModelNotInFavorites = Fault.NotFound
    (
        title: "Preference.ModelNotInFavorites",
        detail: "The specified model was not found in the user's favorites."
    );

    public static readonly Fault MemoryAlreadyEnabled = Fault.Conflict
    (
        title: "Preference.MemoryAlreadyEnabled",
        detail: "Memory is already enabled."
    );

    public static readonly Fault MemoryAlreadyDisabled = Fault.Conflict
    (
        title: "Preference.MemoryAlreadyDisabled",
        detail: "Memory is already disabled."
    );
}