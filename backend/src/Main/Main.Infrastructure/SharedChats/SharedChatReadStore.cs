using System.Data.Common;
using System.Text.Json;

using Dapper;

using Main.Application.Abstractions.SharedChats;
using Main.Application.Queries.SharedChats.GetSharedChat;

using Microsoft.Extensions.Caching.Distributed;

using SharedKernel.Application.Data;

namespace Main.Infrastructure.SharedChats;

internal sealed class SharedChatReadStore(IDbConnectionFactory dbConnectionFactory, IDistributedCache cache)
    : ISharedChatReadStore
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };
    private const string CacheKeyPrefix = "shared-chat:";

    private const string SharedChatSql = """
                                         SELECT
                                             id as Id,
                                             source_chat_id as SourceChatId,
                                             owner_id as OwnerId,
                                             title as Title,
                                             model_id as ModelId,
                                             view_count as ViewCount,
                                             snapshot_at as SnapshotAt,
                                             created_at as CreatedAt
                                         FROM shared_chats
                                         WHERE id = @SharedChatId
                                         """;

    private const string MessagesSql = """
                                       SELECT
                                            sequence_number as SequenceNumber,
                                            message_role as MessageRole,
                                            message_content as MessageContent,
                                            created_at as CreatedAt,
                                            edited_at as EditedAt
                                       FROM shared_chat_messages
                                       WHERE shared_chat_id = @SharedChatId
                                       ORDER BY sequence_number ASC
                                       """;

    public async Task<GetSharedChatResponse?> GetAsync(string sharedChatId, CancellationToken cancellationToken)
    {
        string cacheKey = $"{CacheKeyPrefix}{sharedChatId}";

        string? cached = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (cached is not null)
            return JsonSerializer.Deserialize<GetSharedChatResponse>(cached);

        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        SharedChatReadModel? sharedChat = await connection.QuerySingleOrDefaultAsync<SharedChatReadModel>
        (
            SharedChatSql,
            new { SharedChatId = sharedChatId }
        );

        if (sharedChat is null)
            return null;

        IEnumerable<SharedChatMessageReadModel> messages = await connection.QueryAsync<SharedChatMessageReadModel>
        (
            MessagesSql,
            new { SharedChatId = sharedChatId }
        );

        GetSharedChatResponse response = new
        (
            SharedChat: sharedChat,
            Messages: messages.AsList()
        );

        string serialized = JsonSerializer.Serialize(response);

        await cache.SetStringAsync
        (
            key: cacheKey,
            value: serialized,
            options: CacheOptions,
            token: cancellationToken
        );

        return response;
    }

    public async Task InvalidateCacheAsync(string sharedChatId, CancellationToken cancellationToken)
    {
        string cacheKey = $"{CacheKeyPrefix}{sharedChatId}";

        await cache.RemoveAsync(cacheKey, cancellationToken);
    }
}