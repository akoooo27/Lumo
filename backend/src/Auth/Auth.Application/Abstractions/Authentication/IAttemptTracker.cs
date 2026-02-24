namespace Auth.Application.Abstractions.Authentication;

public interface IAttemptTracker
{
    Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken);

    Task TrackFailedAttemptAsync(string key, CancellationToken cancellationToken);

    Task<bool> IsCooldownActiveAsync(string key, CancellationToken cancellationToken);

    Task SetCooldownAsync(string key, CancellationToken cancellationToken);
}