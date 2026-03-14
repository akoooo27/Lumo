using FluentAssertions;

using Main.Domain.Aggregates;
using Main.Domain.Constants;
using Main.Domain.Enums;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Tests.Aggregates;

public sealed class SharedChatTests
{
    private static readonly DateTimeOffset UtcNow = DateTimeOffset.UtcNow;
    private static readonly Guid ValidOwnerId = Guid.NewGuid();
    private const string ValidTitle = "My Shared Chat";
    private const string ValidModelId = "claude-3-haiku";
    private static readonly SharedChatId ValidSharedChatId = SharedChatId.UnsafeFrom("sht_01JGX123456789012345678901");
    private static readonly ChatId ValidSourceChatId = ChatId.UnsafeFrom("cht_01JGX123456789012345678901");

    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        Outcome<SharedChat> outcome = SharedChat.Create(
            ValidSharedChatId,
            ValidSourceChatId,
            ValidOwnerId,
            ValidTitle,
            ValidModelId,
            UtcNow);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Id.Should().Be(ValidSharedChatId);
        outcome.Value.SourceChatId.Should().Be(ValidSourceChatId);
        outcome.Value.OwnerId.Should().Be(ValidOwnerId);
        outcome.Value.Title.Should().Be(ValidTitle);
        outcome.Value.ModelId.Should().Be(ValidModelId);
        outcome.Value.ViewCount.Should().Be(0);
        outcome.Value.SnapshotAt.Should().Be(UtcNow);
        outcome.Value.CreatedAt.Should().Be(UtcNow);
        outcome.Value.UpdatedAt.Should().Be(UtcNow);
        outcome.Value.SharedChatMessages.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptySourceChatId_ShouldReturnFailure()
    {
        ChatId emptySourceChatId = ChatId.UnsafeFrom(string.Empty);

        Outcome<SharedChat> outcome = SharedChat.Create(
            ValidSharedChatId,
            emptySourceChatId,
            ValidOwnerId,
            ValidTitle,
            ValidModelId,
            UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SharedChatFaults.SourceChatIdRequired);
    }

    [Fact]
    public void Create_WithEmptyOwnerId_ShouldReturnFailure()
    {
        Outcome<SharedChat> outcome = SharedChat.Create(
            ValidSharedChatId,
            ValidSourceChatId,
            Guid.Empty,
            ValidTitle,
            ValidModelId,
            UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SharedChatFaults.OwnerIdRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyTitle_ShouldReturnFailure(string? title)
    {
        Outcome<SharedChat> outcome = SharedChat.Create(
            ValidSharedChatId,
            ValidSourceChatId,
            ValidOwnerId,
            title!,
            ValidModelId,
            UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SharedChatFaults.TitleRequired);
    }

    [Fact]
    public void Create_WithTooLongTitle_ShouldReturnFailure()
    {
        string title = new('a', ChatConstants.MaxTitleLength + 1);

        Outcome<SharedChat> outcome = SharedChat.Create(
            ValidSharedChatId,
            ValidSourceChatId,
            ValidOwnerId,
            title,
            ValidModelId,
            UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SharedChatFaults.TitleTooLong);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyModelId_ShouldReturnFailure(string? modelId)
    {
        Outcome<SharedChat> outcome = SharedChat.Create(
            ValidSharedChatId,
            ValidSourceChatId,
            ValidOwnerId,
            ValidTitle,
            modelId!,
            UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(SharedChatFaults.ModelIdRequired);
    }

    #endregion

    #region AddMessages Tests

    [Fact]
    public void AddMessages_WithValidMessages_ShouldAddMessages()
    {
        SharedChat sharedChat = SharedChat.Create(
            ValidSharedChatId,
            ValidSourceChatId,
            ValidOwnerId,
            ValidTitle,
            ValidModelId,
            UtcNow).Value;

        List<SharedChatMessage> messages =
        [
            new SharedChatMessage(0, MessageRole.User, "Hello", UtcNow, UtcNow),
            new SharedChatMessage(1, MessageRole.Assistant, "Hi there!", UtcNow.AddSeconds(1), UtcNow.AddSeconds(1))
        ];
        DateTimeOffset updateTime = UtcNow.AddHours(1);

        sharedChat.AddMessages(messages, updateTime);

        sharedChat.SharedChatMessages.Should().HaveCount(2);
        sharedChat.SharedChatMessages[0].MessageContent.Should().Be("Hello");
        sharedChat.SharedChatMessages[1].MessageContent.Should().Be("Hi there!");
        sharedChat.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void AddMessages_ShouldAppendToExistingMessages()
    {
        SharedChat sharedChat = SharedChat.Create(
            ValidSharedChatId,
            ValidSourceChatId,
            ValidOwnerId,
            ValidTitle,
            ValidModelId,
            UtcNow).Value;

        List<SharedChatMessage> initialMessages =
        [
            new SharedChatMessage(0, MessageRole.User, "First", UtcNow, UtcNow)
        ];
        sharedChat.AddMessages(initialMessages, UtcNow);

        List<SharedChatMessage> additionalMessages =
        [
            new SharedChatMessage(1, MessageRole.Assistant, "Second", UtcNow.AddSeconds(1), UtcNow.AddSeconds(1))
        ];
        sharedChat.AddMessages(additionalMessages, UtcNow.AddHours(1));

        sharedChat.SharedChatMessages.Should().HaveCount(2);
    }

    [Fact]
    public void AddMessages_WithNull_ShouldThrowArgumentNullException()
    {
        SharedChat sharedChat = CreateValidSharedChat();

        Action act = () => sharedChat.AddMessages(null!, UtcNow);

        act.Should().Throw<ArgumentNullException>().WithParameterName("messages");
    }

    #endregion

    #region RefreshMessages Tests

    [Fact]
    public void RefreshMessages_WithValidMessages_ShouldReplaceAllMessages()
    {
        SharedChat sharedChat = SharedChat.Create(
            ValidSharedChatId,
            ValidSourceChatId,
            ValidOwnerId,
            ValidTitle,
            ValidModelId,
            UtcNow).Value;

        List<SharedChatMessage> initialMessages =
        [
            new SharedChatMessage(0, MessageRole.User, "Old message", UtcNow, UtcNow)
        ];
        sharedChat.AddMessages(initialMessages, UtcNow);

        List<SharedChatMessage> newMessages =
        [
            new SharedChatMessage(0, MessageRole.User, "New message 1", UtcNow.AddHours(1), UtcNow.AddHours(1)),
            new SharedChatMessage(1, MessageRole.Assistant, "New message 2", UtcNow.AddHours(1).AddSeconds(1), UtcNow.AddHours(1).AddSeconds(1))
        ];
        DateTimeOffset refreshTime = UtcNow.AddHours(2);

        sharedChat.RefreshMessages(newMessages, refreshTime);

        sharedChat.SharedChatMessages.Should().HaveCount(2);
        sharedChat.SharedChatMessages[0].MessageContent.Should().Be("New message 1");
        sharedChat.SharedChatMessages[1].MessageContent.Should().Be("New message 2");
        sharedChat.SnapshotAt.Should().Be(refreshTime);
        sharedChat.UpdatedAt.Should().Be(refreshTime);
    }

    [Fact]
    public void RefreshMessages_WithEmptyList_ShouldClearAllMessages()
    {
        SharedChat sharedChat = SharedChat.Create(
            ValidSharedChatId,
            ValidSourceChatId,
            ValidOwnerId,
            ValidTitle,
            ValidModelId,
            UtcNow).Value;

        List<SharedChatMessage> initialMessages =
        [
            new SharedChatMessage(0, MessageRole.User, "Message", UtcNow, UtcNow)
        ];
        sharedChat.AddMessages(initialMessages, UtcNow);

        sharedChat.RefreshMessages([], UtcNow.AddHours(1));

        sharedChat.SharedChatMessages.Should().BeEmpty();
    }

    [Fact]
    public void RefreshMessages_ShouldUpdateSnapshotAt()
    {
        SharedChat sharedChat = SharedChat.Create(
            ValidSharedChatId,
            ValidSourceChatId,
            ValidOwnerId,
            ValidTitle,
            ValidModelId,
            UtcNow).Value;

        DateTimeOffset originalSnapshotAt = sharedChat.SnapshotAt;
        DateTimeOffset refreshTime = UtcNow.AddHours(5);

        sharedChat.RefreshMessages([], refreshTime);

        sharedChat.SnapshotAt.Should().Be(refreshTime);
        sharedChat.SnapshotAt.Should().NotBe(originalSnapshotAt);
    }

    [Fact]
    public void RefreshMessages_WithNull_ShouldThrowArgumentNullException()
    {
        SharedChat sharedChat = CreateValidSharedChat();

        Action act = () => sharedChat.RefreshMessages(null!, UtcNow);

        act.Should().Throw<ArgumentNullException>().WithParameterName("messages");
    }

    #endregion

    #region Identity Tests

    [Fact]
    public void Create_WithDifferentIds_ShouldHaveDifferentIds()
    {
        SharedChatId anotherSharedChatId = SharedChatId.UnsafeFrom("sht_01JGX098765432109876543210");

        SharedChat sharedChat1 = SharedChat.Create(
            ValidSharedChatId,
            ValidSourceChatId,
            ValidOwnerId,
            ValidTitle,
            ValidModelId,
            UtcNow).Value;

        SharedChat sharedChat2 = SharedChat.Create(
            anotherSharedChatId,
            ValidSourceChatId,
            ValidOwnerId,
            ValidTitle,
            ValidModelId,
            UtcNow).Value;

        sharedChat1.Id.Should().NotBe(sharedChat2.Id);
    }

    #endregion

    private static SharedChat CreateValidSharedChat() =>
        SharedChat.Create(
            ValidSharedChatId,
            ValidSourceChatId,
            ValidOwnerId,
            ValidTitle,
            ValidModelId,
            UtcNow).Value;
}