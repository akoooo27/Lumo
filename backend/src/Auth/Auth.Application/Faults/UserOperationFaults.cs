using SharedKernel;

namespace Auth.Application.Faults;

internal static class UserOperationFaults
{
    internal static readonly Fault EmailAlreadyInUse = Fault.Conflict
    (
        title: "User.EmailAlreadyInUse",
        detail: "The provided email address is already associated with an existing account."
    );

    internal static readonly Fault NotFound = Fault.NotFound
    (
        title: "User.NotFound",
        detail: "The user with the provided ID was not found."
    );

    internal static readonly Fault AvatarNotFound = Fault.NotFound
    (
        title: "User.AvatarNotFound",
        detail: "The avatar file with the provided key was not found in storage."
    );

    internal static readonly Fault AvatarForbidden = Fault.Forbidden
    (
        "User.AvatarForbidden",
        "The avatar key does not belong to the current user."
    );
}