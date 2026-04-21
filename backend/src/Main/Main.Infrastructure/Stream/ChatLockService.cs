using Main.Application.Abstractions.Stream;
using Main.Infrastructure.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

namespace Main.Infrastructure.Stream;

internal sealed class ChatLockService(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<ChatStreamingOptions> chatStreamingOptions,
    ILogger<ChatLockService> logger) : IChatLockService
{
    private readonly ChatStreamingOptions _chatStreamingOptions = chatStreamingOptions.Value;

    private const string LockKeyPrefix = "chat:lock:";

    public async Task<bool> TryAcquireLockAsync(string chatId, string ownerId, CancellationToken cancellationToken)
    {
        string lockKey = $"{LockKeyPrefix}{chatId}";

        IDatabase db = connectionMultiplexer.GetDatabase();

        try
        {
            bool acquired = await db.StringSetAsync
            (
                key: lockKey,
                value: ownerId,
                expiry: _chatStreamingOptions.GenerationLockExpiration,
                when: When.NotExists
            );

            if (acquired)
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Acquired lock for chatId: {ChatId}", chatId);
            }
            else
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Failed to acquire lock for chatId: {ChatId}", chatId);
            }

            return acquired;
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Error acquiring lock for chatId: {ChatId}", chatId);
            return false;
        }
    }

    public async Task ReleaseLockAsync(string chatId, string ownerId, CancellationToken cancellationToken)
    {
        string lockKey = $"{LockKeyPrefix}{chatId}";

        IDatabase db = connectionMultiplexer.GetDatabase();

        try
        {
            ITransaction transaction = db.CreateTransaction();

            transaction.AddCondition(Condition.StringEqual(lockKey, ownerId));

            _ = transaction.KeyDeleteAsync(lockKey);

            bool committed = await transaction.ExecuteAsync();

            if (committed)
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Released lock for chatId: {ChatId}", chatId);
            }
            else
                logger.LogWarning("Lock not owned by this request for chatId: {ChatId}", chatId);
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Error releasing lock for chatId: {ChatId}", chatId);
            throw;
        }
    }

    public async Task<bool> IsGeneratingAsync(string chatId, CancellationToken cancellationToken)
    {
        string lockKey = $"{LockKeyPrefix}{chatId}";

        IDatabase db = connectionMultiplexer.GetDatabase();

        try
        {
            return await db.KeyExistsAsync(lockKey);
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Error checking lock for chatId: {ChatId}", chatId);
            return false;
        }
    }

    public async Task RequestCancellationAsync(string streamId, CancellationToken cancellationToken)
    {
        string cancellationKey = $"{StreamConstants.CancelKeyPrefix}{streamId}";

        IDatabase db = connectionMultiplexer.GetDatabase();

        try
        {
            await db.StringSetAsync
            (
                key: cancellationKey,
                value: "1",
                expiry: TimeSpan.FromSeconds(60)
            );

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Cancellation requested for streamId: {StreamId}", streamId);
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Error requesting cancellation for streamId: {StreamId}", streamId);
            throw;
        }
    }

    public async Task<bool> IsCancellationRequestedAsync(string streamId, CancellationToken cancellationToken)
    {
        string cancellationKey = $"{StreamConstants.CancelKeyPrefix}{streamId}";

        IDatabase db = connectionMultiplexer.GetDatabase();

        try
        {
            return await db.KeyExistsAsync(cancellationKey);
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Error checking cancellation for streamId: {StreamId}", streamId);
            return false;
        }
    }

    public async Task ClearCancellationAsync(string streamId, CancellationToken cancellationToken)
    {
        string cancellationKey = $"{StreamConstants.CancelKeyPrefix}{streamId}";

        IDatabase db = connectionMultiplexer.GetDatabase();

        try
        {
            await db.KeyDeleteAsync(cancellationKey);
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Error clearing cancellation for streamId: {StreamId}", streamId);
        }
    }
}