using System.Diagnostics.CodeAnalysis;

using Main.Domain.Constants;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Aggregates;

public sealed class SharedChat : AggregateRoot<SharedChatId>
{
    private readonly List<SharedChatMessage> _sharedChatMessages = [];

    public ChatId SourceChatId { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string ModelId { get; private set; } = string.Empty;

    public int ViewCount { get; private set; }

    public DateTimeOffset SnapshotAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<SharedChatMessage> SharedChatMessages => _sharedChatMessages.AsReadOnly();

    private SharedChat() { } // For EF Core

    [SetsRequiredMembers]
    private SharedChat
    (
        SharedChatId id,
        ChatId sourceChatId,
        Guid ownerId,
        string title,
        string modelId,
        DateTimeOffset utcNow
    )
    {
        Id = id;
        SourceChatId = sourceChatId;
        OwnerId = ownerId;
        Title = title;
        ModelId = modelId;
        ViewCount = 0;
        SnapshotAt = utcNow;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static Outcome<SharedChat> Create
    (
        SharedChatId id,
        ChatId sourceChatId,
        Guid ownerId,
        string title,
        string modelId,
        DateTimeOffset utcNow
    )
    {
        if (sourceChatId.IsEmpty)
            return SharedChatFaults.SourceChatIdRequired;

        if (ownerId == Guid.Empty)
            return SharedChatFaults.OwnerIdRequired;

        if (string.IsNullOrWhiteSpace(title))
            return SharedChatFaults.TitleRequired;

        if (title.Length > ChatConstants.MaxTitleLength)
            return SharedChatFaults.TitleTooLong;

        if (string.IsNullOrWhiteSpace(modelId))
            return SharedChatFaults.ModelIdRequired;

        SharedChat sharedChat = new
        (
            id: id,
            sourceChatId: sourceChatId,
            ownerId: ownerId,
            title: title,
            modelId: modelId,
            utcNow: utcNow
        );

        return sharedChat;
    }

    public void AddMessages(IReadOnlyList<SharedChatMessage> messages, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(messages);

        _sharedChatMessages.AddRange(messages);
        UpdatedAt = utcNow;
    }

    public void RefreshMessages(IReadOnlyList<SharedChatMessage> messages, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(messages);

        _sharedChatMessages.Clear();
        _sharedChatMessages.AddRange(messages);

        SnapshotAt = utcNow;
        UpdatedAt = utcNow;
    }
}