namespace Notifications.Api.ReadModels;

internal sealed class User
{
    public required Guid UserId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string EmailAddress { get; init; } = string.Empty;

    public string? AvatarKey { get; init; }
}