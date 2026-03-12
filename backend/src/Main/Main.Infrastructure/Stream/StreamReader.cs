using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

using Main.Application.Abstractions.Stream;

using StackExchange.Redis;

namespace Main.Infrastructure.Stream;

internal sealed class StreamReader(IConnectionMultiplexer connectionMultiplexer) : IStreamReader
{
    private const int ReadSize = 1000;
    private const int TimeoutSeconds = 30;

    public async IAsyncEnumerable<StreamMessage> ReadStreamAsync(string streamId,
        [EnumeratorCancellation] CancellationToken cancellationToken)

    {
        string streamKey = $"{StreamConstants.StreamKeyPrefix}{streamId}";
        string notifyChannel = $"{StreamConstants.NotifyChannelPrefix}{streamId}";

        IDatabase db = connectionMultiplexer.GetDatabase();
        ISubscriber sub = connectionMultiplexer.GetSubscriber();

        RedisValue lastId = "0-0";

        Channel<bool> notificationChannel = Channel.CreateUnbounded<bool>();

        try
        {
            await sub.SubscribeAsync
            (
                RedisChannel.Literal(notifyChannel),
                (_, _) => notificationChannel.Writer.TryWrite(true)
            );

            // PHASE 1: Read any existing messages (late-joiner scenario)
            StreamEntry[] existingEntries = await db.StreamReadAsync
            (
                key: streamKey,
                position: lastId,
                count: ReadSize
            );

            foreach (StreamEntry entry in existingEntries)
            {
                lastId = entry.Id;
                StreamMessage? message = ParseEntry(entry);

                if (message is not null)
                {
                    yield return message;

                    if (IsTerminalStatus(message))
                        yield break;
                }
            }

            // PHASE 2: Listen for new messages
            while (!cancellationToken.IsCancellationRequested)
            {
                using CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(TimeoutSeconds));
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource
                (
                    token1: cancellationToken,
                    timeoutCts.Token
                );

                try
                {
                    await notificationChannel.Reader.ReadAsync(linkedCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    continue;
                }

                StreamEntry[] entries = await db.StreamReadAsync
                (
                    key: streamKey,
                    position: lastId,
                    count: ReadSize
                );

                if (entries.Length == 0)
                    continue;

                foreach (StreamEntry entry in entries)
                {
                    lastId = entry.Id;
                    StreamMessage? message = ParseEntry(entry);

                    if (message is not null)
                    {
                        yield return message;

                        if (IsTerminalStatus(message))
                            yield break;
                    }
                }
            }
        }
        finally
        {
            notificationChannel.Writer.Complete();

            try
            {
                await sub.UnsubscribeAsync(RedisChannel.Literal(notifyChannel));
            }
            catch (RedisException)
            {
                // Connection already lost
            }
        }
    }

    private static StreamMessage? ParseEntry(StreamEntry entry)
    {
        string? type = entry["type"];
        string? timestamp = entry["timestamp"];

        if (type is null || timestamp is null)
            return null;

        if (!long.TryParse(timestamp, CultureInfo.InvariantCulture, out long timestampMs))
            return null;

        DateTimeOffset ts = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);

        return type switch
        {
            "chunk" => new StreamMessage
            (
                StreamMessageType.Chunk,
                (string?)entry["content"] ?? string.Empty,
                ts,
                ModelName: null,
                Provider: null
            ),
            "status" => new StreamMessage
            (
                StreamMessageType.Status,
                (string?)entry["status"] ?? string.Empty,
                ts,
                ModelName: entry["model_name"],
                Provider: entry["provider"]
            ),
            "tool_call" => new StreamMessage
            (
                StreamMessageType.ToolCall,
                (string?)entry["tool_name"] ?? string.Empty,
                ts,
                ModelName: null,
                Provider: null
            )
            {
                Query = entry["query"]
            },
            "tool_result" => new StreamMessage
            (
                StreamMessageType.ToolCallResult,
                (string?)entry["tool_name"] ?? string.Empty,
                ts,
                ModelName: null,
                Provider: null
            )
            {
                Sources = entry["sources"]
            },
            "thinking" => new StreamMessage
            (
                StreamMessageType.Thinking,
                (string?)entry["phase"] ?? string.Empty,
                ts,
                ModelName: null,
                Provider: null
            ),
            _ => null
        };
    }

    private static bool IsTerminalStatus(StreamMessage message) =>
        message is { Type: StreamMessageType.Status, Content: "done" or "failed" };
}