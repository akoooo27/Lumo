using System.Data.Common;

using Dapper;

using Main.Application.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Chats.GetMessages;

internal sealed class GetMessageHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetMessagesQuery, GetMessagesResponse>
{
    private const string ChatExistsSql = """
                                         SELECT EXISTS
                                         (
                                            SELECT 1 FROM chats
                                            where id = @ChatId and user_id = @UserId
                                         )
                                         """;

    private const string GetMessagesSql = """
                                          SELECT
                                            id as Id,
                                            chat_id as ChatId,
                                            message_role as MessageRole,
                                            message_content as MessageContent,
                                            input_token_count as InputTokenCount,
                                            output_token_count as OutputTokenCount,
                                            total_token_count as TotalTokenCount,
                                            sequence_number as SequenceNumber,
                                            sources_json as SourcesJson,
                                            created_at as CreatedAt,
                                            edited_at as EditedAt
                                          FROM messages
                                          WHERE chat_id = @ChatId
                                          ORDER BY sequence_number desc
                                          LIMIT @Limit
                                          """;

    private const string GetMessagesWithCursorSql = """
                                                    SELECT
                                                      id as Id,
                                                      chat_id as ChatId,
                                                      message_role as MessageRole,
                                                      message_content as MessageContent,
                                                      input_token_count as InputTokenCount,
                                                      output_token_count as OutputTokenCount,
                                                      total_token_count as TotalTokenCount,
                                                      sequence_number as SequenceNumber,
                                                      sources_json as SourcesJson,
                                                      created_at as CreatedAt,
                                                      edited_at as EditedAt
                                                    FROM messages
                                                    WHERE chat_id = @ChatId AND sequence_number < @Cursor
                                                    ORDER BY sequence_number desc
                                                    LIMIT @Limit
                                                    """;

    public async ValueTask<Outcome<GetMessagesResponse>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        Outcome<ChatId> chatIdOutcome = ChatId.From(request.ChatId);

        if (chatIdOutcome.IsFailure)
            return chatIdOutcome.Fault;

        ChatId chatId = chatIdOutcome.Value;
        Guid userId = userContext.UserId;

        await using DbConnection connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        bool chatExists = await connection.QueryFirstAsync<bool>
        (
            ChatExistsSql,
            new { ChatId = chatId.Value, UserId = userId }
        );

        if (!chatExists)
            return ChatOperationFaults.NotFound;

        int fetchLimit = request.Limit + 1;

        IEnumerable<MessageReadModel> messages = request.Cursor.HasValue
            ? await connection.QueryAsync<MessageReadModel>
            (
                GetMessagesWithCursorSql,
                new { ChatId = chatId.Value, Cursor = request.Cursor.Value, Limit = fetchLimit }
            )
            : await connection.QueryAsync<MessageReadModel>
            (
                GetMessagesSql,
                new { ChatId = chatId.Value, Limit = fetchLimit }
            );

        List<MessageReadModel> messageList = messages.AsList();

        bool hasMore = messageList.Count > request.Limit;

        if (hasMore)
            messageList.RemoveAt(messageList.Count - 1);

        int? nextCursor = hasMore ? messageList[^1].SequenceNumber : null;

        PaginationInfo paginationInfo = new
        (
            HasMore: hasMore,
            NextCursor: nextCursor,
            Limit: request.Limit
        );

        messageList.Reverse();

        GetMessagesResponse response = new
        (
            Messages: messageList,
            Pagination: paginationInfo
        );

        return response;
    }
}