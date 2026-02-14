using Auth.Application.Abstractions.Authentication;
using Auth.Domain.Constants;

using StackExchange.Redis;

namespace Auth.Infrastructure.Authentication;

internal sealed class AttemptTracker(IConnectionMultiplexer connectionMultiplexer) : IAttemptTracker
{
    private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(AttemptConstants.LockoutWindowMinutes);
    private static readonly TimeSpan CooldownDuration = TimeSpan.FromSeconds(AttemptConstants.LoginCooldownSeconds);
    private const string Prefix = "otp-attempts:";
    private const string CooldownPrefix = "login-cooldown:";

    public async Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken)
    {
        IDatabase db = connectionMultiplexer.GetDatabase();
        RedisValue value = await db.StringGetAsync($"{Prefix}{key}");

        return value.HasValue && (int)value >= AttemptConstants.MaxVerificationAttempts;
    }

    public async Task TrackFailedAttemptAsync(string key, CancellationToken cancellationToken)
    {
        IDatabase db = connectionMultiplexer.GetDatabase();
        string cacheKey = $"{Prefix}{key}";

        await db.StringIncrementAsync(cacheKey);
        await db.KeyExpireAsync(cacheKey, LockoutWindow, ExpireWhen.HasNoExpiry);
    }

    public async Task<bool> IsCooldownActiveAsync(string key, CancellationToken cancellationToken)
    {
        IDatabase db = connectionMultiplexer.GetDatabase();

        return await db.KeyExistsAsync($"{CooldownPrefix}{key}");
    }

    public async Task SetCooldownAsync(string key, CancellationToken cancellationToken)
    {
        IDatabase db = connectionMultiplexer.GetDatabase();

        await db.StringSetAsync($"{CooldownPrefix}{key}", 1, CooldownDuration);
    }
}