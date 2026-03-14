using System.Diagnostics.CodeAnalysis;

using Main.Domain.Enums;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Entities;

public sealed class Message : Entity<MessageId>
{
    public ChatId ChatId { get; private set; }

    public MessageRole MessageRole { get; private set; }

    public string MessageContent { get; private set; } = string.Empty;

    public long? InputTokenCount { get; private set; }

    public long? OutputTokenCount { get; private set; }

    public long? TotalTokenCount { get; private set; }

    public int SequenceNumber { get; private set; }

    public string? SourcesJson { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset EditedAt { get; private set; }

    private Message() { } // For EF Core

    [SetsRequiredMembers]
    private Message
    (
        MessageId id,
        ChatId chatId,
        MessageRole messageRole,
        string messageContent,
        int sequenceNumber,
        DateTimeOffset utcNow
    )
    {
        Id = id;
        ChatId = chatId;
        MessageRole = messageRole;
        MessageContent = messageContent;
        InputTokenCount = null;
        OutputTokenCount = null;
        TotalTokenCount = null;
        SequenceNumber = sequenceNumber;
        CreatedAt = utcNow;
        EditedAt = utcNow;
    }

    internal static Outcome<Message> Create
    (
        MessageId id,
        ChatId chatId,
        MessageRole messageRole,
        string messageContent,
        int sequenceNumber,
        DateTimeOffset utcNow
    )
    {
        if (id.IsEmpty)
            return MessageFaults.MessageIdRequired;

        if (chatId.IsEmpty)
            return MessageFaults.ChatIdRequired;

        if (!Enum.IsDefined(messageRole))
            return MessageFaults.InvalidMessageRole;

        if (string.IsNullOrWhiteSpace(messageContent))
            return MessageFaults.MessageContentRequired;

        if (sequenceNumber < 0)
            return MessageFaults.InvalidSequenceNumber;

        Message message = new
        (
            id: id,
            chatId: chatId,
            messageRole: messageRole,
            messageContent: messageContent,
            sequenceNumber: sequenceNumber,
            utcNow: utcNow
        );

        return message;
    }

    internal Outcome EditContent(string newContent, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(newContent))
            return MessageFaults.MessageContentRequired;

        MessageContent = newContent;
        EditedAt = utcNow;

        return Outcome.Success();
    }

    internal Outcome SetTokenUsage
    (
        long inputTokenCount,
        long outputTokenCount,
        long totalTokenCount
    )
    {
        if (inputTokenCount < 0 || outputTokenCount < 0 || totalTokenCount < 0)
            return MessageFaults.NegativeTokenCount;

        InputTokenCount = inputTokenCount;
        OutputTokenCount = outputTokenCount;
        TotalTokenCount = totalTokenCount;

        return Outcome.Success();
    }

    internal Outcome SetSourcesJson(string sourcesJson)
    {
        if (string.IsNullOrWhiteSpace(sourcesJson))
            return MessageFaults.SourcesRequired;

        if (MessageRole != MessageRole.Assistant)
            return MessageFaults.MessageSourceNotAllowed;

        SourcesJson = sourcesJson;

        return Outcome.Success();
    }
}