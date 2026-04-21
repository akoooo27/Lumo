using SharedKernel;

namespace Main.Domain.Faults;

public static class GoogleConnectionFaults
{
    public static readonly Fault UserIdRequired = Fault.Validation
    (
        title: "GoogleConnection.UserIdRequired",
        detail: "A user ID is required to create a Google connection."
    );

    public static readonly Fault AccessTokenRequired = Fault.Validation
    (
        title: "GoogleConnection.AccessTokenRequired",
        detail: "A protected access token is required."
    );

    public static readonly Fault RefreshTokenRequired = Fault.Validation
    (
        title: "GoogleConnection.RefreshTokenRequired",
        detail: "A protected refresh token is required."
    );

    public static readonly Fault GoogleEmailRequired = Fault.Validation
    (
        title: "GoogleConnection.GoogleEmailRequired",
        detail: "A Google email address is required."
    );
}