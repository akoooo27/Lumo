namespace Auth.Domain.Enums;

public enum SessionRevokeReason
{
    None = 0,
    UserLogout = 1,
    EmailChange = 2,
    AccountRecovery = 3,
    OldRefreshTokenUsed = 4
}