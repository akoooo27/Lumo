using Main.Domain.Constants;

using SharedKernel;

namespace Main.Domain.Faults;

public static class FolderFaults
{
    public static readonly Fault UserIdRequired = Fault.Validation
    (
        title: "Folder.UserIdRequired",
        detail: "User ID is required."
    );

    public static readonly Fault NameRequired = Fault.Validation
    (
        title: "Folder.NameRequired",
        detail: "Folder name is required."
    );

    public static readonly Fault NameTooLong = Fault.Validation
    (
        title: "Folder.NameTooLong",
        detail: $"Folder name must not exceed {FolderConstants.MaxNameLength} characters."
    );

    public static readonly Fault InvalidSortOrder = Fault.Validation
    (
        title: "Folder.InvalidSortOrder",
        detail: "Sort order must not be negative."
    );
}