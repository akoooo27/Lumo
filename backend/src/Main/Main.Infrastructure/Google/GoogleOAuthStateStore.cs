using Main.Application.Abstractions.Google;

using StackExchange.Redis;

namespace Main.Infrastructure.Google;

internal sealed class GoogleOAuthStateStore(IConnectionMultiplexer connectionMultiplexer) : IGoogleOAuthStateStore
{
    private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(10);
    private const string Prefix = "google-oauth-state:";

    public async Task<string> GenerateAndStoreAsync(Guid userId, CancellationToken cancellationToken)
    {
        string state = Guid.CreateVersion7().ToString();
        IDatabase db = connectionMultiplexer.GetDatabase();

        await db.StringSetAsync($"{Prefix}{state}", userId.ToString(), StateTtl);

        return state;
    }

    public async Task<Guid?> ValidateAndConsumeAsync(string state, CancellationToken cancellationToken)
    {
        IDatabase db = connectionMultiplexer.GetDatabase();

        RedisValue value = await db.StringGetDeleteAsync($"{Prefix}{state}");

        if (!value.HasValue)
            return null;

        return Guid.TryParse(value.ToString(), out Guid userId) ? userId : null;
    }
}