using SharedKernel;

namespace Auth.Application.Faults;

internal static class LoginRequestOperationFaults
{
    internal static readonly Fault TooManyAttempts = Fault.TooManyRequests
    (
        title: "LoginRequest.TooManyAttempts",
        detail: "Too many verification attempts. Please request a new login code."
    );
}