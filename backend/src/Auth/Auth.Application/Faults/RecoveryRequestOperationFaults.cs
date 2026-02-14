using SharedKernel;

namespace Auth.Application.Faults;

internal static class RecoveryRequestOperationFaults
{
    internal static readonly Fault NotFound = Fault.NotFound
    (
        title: "RecoveryRequest.NotFound",
        detail: "The recovery request was not found or has expired."
    );

    internal static readonly Fault InvalidOrExpired = Fault.Unauthorized
    (
        title: "RecoveryRequest.InvalidOrExpired",
        detail: "The recovery request is invalid or has expired."
    );

    internal static readonly Fault TooManyAttempts = Fault.TooManyRequests
    (
        title: "RecoveryRequest.TooManyAttempts",
        detail: "Too many verification attempts. Please start a new recovery request."
    );
}