namespace Auth.Domain.Constants;

public static class AttemptConstants
{
    public const int MaxVerificationAttempts = 5;

    public const int LockoutWindowMinutes = 10;

    public const int LoginCooldownSeconds = 60;
}