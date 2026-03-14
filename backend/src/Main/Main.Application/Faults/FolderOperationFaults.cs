using SharedKernel;

namespace Main.Application.Faults;

internal static class FolderOperationFaults
{
    internal static readonly Fault NotFound = Fault.NotFound
    (
        title: "Folder.NotFound",
        detail: "The specified folder was not found."
    );

    internal static readonly Fault DuplicateName = Fault.Conflict
    (
        title: "Folder.DuplicateName",
        detail: "A folder with this name already exists."
    );

    internal static readonly Fault MaxFoldersReached = Fault.Conflict
    (
        title: "Folder.MaxFoldersReached",
        detail: "You have reached the maximum number of folders."
    );

    internal static readonly Fault FolderNotOwned = Fault.Forbidden
    (
        title: "Folder.NotOwned",
        detail: "The specified folder does not belong to you."
    );
}