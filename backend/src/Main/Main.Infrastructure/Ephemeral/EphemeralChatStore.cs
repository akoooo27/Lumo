using System.Text.Json;

using Main.Application.Abstractions.Ephemeral;
using Main.Domain.Constants;
using Main.Domain.Models;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Main.Infrastructure.Ephemeral;

internal sealed class EphemeralChatStore(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<EphemeralChatStore> logger) : IEphemeralChatStore
{
    private static readonly TimeSpan Expiration = TimeSpan.FromHours(1);
    private const string KeyPrefix = "ephemeral:chat:";

    public async Task<EphemeralChat?> GetAsync(string ephemeralChatId, CancellationToken cancellationToken)
    {
        IDatabase database = connectionMultiplexer.GetDatabase();

        RedisValue redisValue = await database.StringGetAsync($"{KeyPrefix}{ephemeralChatId}");

        if (redisValue.IsNullOrEmpty)
            return null;

        try
        {
            return JsonSerializer.Deserialize<EphemeralChat>(redisValue.ToString());
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize ephemeral chat {EphemeralChatId}, treating as cache miss",
                ephemeralChatId);
            return null;
        }
    }

    public async Task SaveAsync(EphemeralChat ephemeralChat, CancellationToken cancellationToken)
    {
        IDatabase database = connectionMultiplexer.GetDatabase();

        if (ephemeralChat.Messages.Count > ChatConstants.MaxContextMessages)
        {
            ephemeralChat.Messages = ephemeralChat.Messages
                .OrderByDescending(m => m.SequenceNumber)
                .Take(ChatConstants.MaxContextMessages)
                .OrderBy(m => m.SequenceNumber)
                .ToList();
        }

        string json = JsonSerializer.Serialize(ephemeralChat);

        await database.StringSetAsync
        (
            key: $"{KeyPrefix}{ephemeralChat.EphemeralChatId}",
            value: json,
            expiry: Expiration
        );
    }

    public async Task DeleteAsync(string ephemeralChatId, CancellationToken cancellationToken)
    {
        IDatabase database = connectionMultiplexer.GetDatabase();

        await database.KeyDeleteAsync($"{KeyPrefix}{ephemeralChatId}");
    }
}