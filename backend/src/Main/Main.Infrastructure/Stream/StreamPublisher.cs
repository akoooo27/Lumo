using System.Globalization;

using Main.Application.Abstractions.Stream;

using Microsoft.Extensions.Logging;

using SharedKernel;

using StackExchange.Redis;

namespace Main.Infrastructure.Stream;

internal sealed class StreamPublisher(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<StreamPublisher> logger,
    IDateTimeProvider dateTimeProvider) : IStreamPublisher
{
    public async Task PublishStatusAsync
    (
        string streamId,
        StreamStatus status,
        CancellationToken cancellationToken,
        string? fault = null,
        string? modelName = null,
        string? provider = null
    )
    {
        string streamKey = $"{StreamConstants.StreamKeyPrefix}{streamId}";
        string notifyChannel = $"{StreamConstants.NotifyChannelPrefix}{streamId}";

        IDatabase db = connectionMultiplexer.GetDatabase();
        ISubscriber pub = connectionMultiplexer.GetSubscriber();

        try
        {
            List<NameValueEntry> entries =
            [
                new NameValueEntry("type", "status"),
#pragma warning disable CA1308
                new NameValueEntry("status", status.ToString().ToLowerInvariant()),
#pragma warning restore CA1308
                new NameValueEntry("timestamp",
                    dateTimeProvider.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture))

            ];

            if (!string.IsNullOrWhiteSpace(fault))
                entries.Add(new NameValueEntry("fault", fault));

            if (!string.IsNullOrWhiteSpace(modelName))
                entries.Add(new NameValueEntry("model_name", modelName));

            if (!string.IsNullOrWhiteSpace(provider))
                entries.Add(new NameValueEntry("provider", provider));

            await db.StreamAddAsync(streamKey, [.. entries]);
            await pub.PublishAsync(RedisChannel.Literal(notifyChannel), "status");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish status for stream {StreamId}", streamId);
            throw;
        }
    }

    public async Task SetStreamExpirationAsync(string streamId, TimeSpan expiration, CancellationToken cancellationToken)
    {
        string streamKey = $"{StreamConstants.StreamKeyPrefix}{streamId}";

        IDatabase db = connectionMultiplexer.GetDatabase();

        await db.KeyExpireAsync(streamKey, expiration);
    }

    public async Task PublishChunkAsync(string streamId, string messageContent, CancellationToken cancellationToken)
    {
        string streamKey = $"{StreamConstants.StreamKeyPrefix}{streamId}";
        string notifyChannel = $"{StreamConstants.NotifyChannelPrefix}{streamId}";

        IDatabase db = connectionMultiplexer.GetDatabase();
        ISubscriber pub = connectionMultiplexer.GetSubscriber();

        try
        {
            List<NameValueEntry> entries =
            [
                new NameValueEntry("type", "chunk"),
                new NameValueEntry("content", messageContent),
                new NameValueEntry("timestamp",
                    dateTimeProvider.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture))
            ];

            await db.StreamAddAsync(streamKey, [.. entries]);
            await pub.PublishAsync(RedisChannel.Literal(notifyChannel), "chunk");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish chunk for stream {StreamId}", streamId);
            throw;
        }
    }

    public async Task PublishToolCallAsync(string streamId, string toolName, string? query,
        CancellationToken cancellationToken)
    {
        string streamKey = $"{StreamConstants.StreamKeyPrefix}{streamId}";
        string notifyChannel = $"{StreamConstants.NotifyChannelPrefix}{streamId}";

        IDatabase db = connectionMultiplexer.GetDatabase();
        ISubscriber pub = connectionMultiplexer.GetSubscriber();

        try
        {
            List<NameValueEntry> entries =
            [
                new NameValueEntry("type", "tool_call"),
                new NameValueEntry("tool_name", toolName),
                new NameValueEntry("timestamp",
                    dateTimeProvider.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture))
            ];

            if (!string.IsNullOrWhiteSpace(query))
                entries.Add(new NameValueEntry("query", query));

            await db.StreamAddAsync(streamKey, [.. entries]);
            await pub.PublishAsync(RedisChannel.Literal(notifyChannel), "tool_call");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish tool call for stream {StreamId}", streamId);
            throw;
        }
    }

    public async Task PublishToolCallResultAsync(string streamId, string toolName, string sourcesJson,
        CancellationToken cancellationToken)
    {
        string streamKey = $"{StreamConstants.StreamKeyPrefix}{streamId}";
        string notifyChannel = $"{StreamConstants.NotifyChannelPrefix}{streamId}";

        IDatabase db = connectionMultiplexer.GetDatabase();
        ISubscriber pub = connectionMultiplexer.GetSubscriber();

        try
        {
            List<NameValueEntry> entries =
            [
                new NameValueEntry("type", "tool_result"),
                new NameValueEntry("tool_name", toolName),
                new NameValueEntry("sources", sourcesJson),
                new NameValueEntry("timestamp",
                    dateTimeProvider.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture))
            ];

            await db.StreamAddAsync(streamKey, [.. entries]);
            await pub.PublishAsync(RedisChannel.Literal(notifyChannel), "tool_result");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish tool call result for stream {StreamId}", streamId);
            throw;
        }
    }

    public async Task PublishThinkingAsync(string streamId, string phase, CancellationToken cancellationToken)
    {
        string streamKey = $"{StreamConstants.StreamKeyPrefix}{streamId}";
        string notifyChannel = $"{StreamConstants.NotifyChannelPrefix}{streamId}";

        IDatabase db = connectionMultiplexer.GetDatabase();
        ISubscriber pub = connectionMultiplexer.GetSubscriber();

        try
        {
            List<NameValueEntry> entries =
            [
                new NameValueEntry("type", "thinking"),
                new NameValueEntry("phase", phase),
                new NameValueEntry("timestamp",
                    dateTimeProvider.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture))
            ];

            await db.StreamAddAsync(streamKey, [.. entries]);
            await pub.PublishAsync(RedisChannel.Literal(notifyChannel), "thinking");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish thinking for stream {StreamId}", streamId);
            throw;
        }
    }
}