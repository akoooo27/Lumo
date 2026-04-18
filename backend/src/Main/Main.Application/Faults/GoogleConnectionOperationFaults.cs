using SharedKernel;

namespace Main.Application.Faults;

internal static class GoogleConnectionOperationFaults
{
    public static readonly Fault InvalidOAuthState = Fault.Problem
    (
        title: "GoogleConnection.InvalidOAuthState",
        detail: "The OAuth state parameter is invalid or expired."
    );

    public static readonly Fault ConnectionNotFound = Fault.NotFound
    (
        title: "GoogleConnection.ConnectionNotFound",
        detail: "No Google connection found for the user."
    );
}