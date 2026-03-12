using SharedKernel;

namespace Main.Application.Faults;

internal static class MemoryOperationFaults
{
    internal static readonly Fault NotFound = Fault.NotFound
    (
        title: "Memory.NotFound",
        detail: "The specified memory was not found."
    );

    internal static readonly Fault NoMemoriesToImport = Fault.Failure
    (
        title: "MemoryImport.NoMemoriesToImport",
        detail: "No valid memories were found in the provided text."
    );
}