using SharedKernel;

namespace Auth.Application.Faults;

internal static class EmailChangeRequestOperationFaults
{
    internal static readonly Fault InvalidOrExpired = Fault.Unauthorized
    (
        title: "EmailChangeRequest.InvalidOrExpired",
        detail: "The email change request is invalid or has expired."
    );

    internal static readonly Fault InvalidToken = Fault.Unauthorized
    (
        title: "EmailChangeRequest.InvalidToken",
        detail: "The verification code is invalid. Please check and try again."
    );

    internal static readonly Fault NotFoundOrNotOwned = Fault.NotFound
    (
        title: "EmailChangeRequest.NotFoundOrNotOwned",
        detail: "The email change request was not found or does not belong to you."
    );

    internal static readonly Fault SameAsCurrentEmail = Fault.Conflict
    (
        title: "EmailChangeRequest.SameAsCurrentEmail",
        detail: "The new email address must be different from your current email."
    );

    internal static readonly Fault TooManyAttempts = Fault.TooManyRequests
    (
        title: "EmailChangeRequest.TooManyAttempts",
        detail: "Too many verification attempts. Please request a new verification code."
    );
}