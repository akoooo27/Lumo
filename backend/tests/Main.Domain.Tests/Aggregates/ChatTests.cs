using FluentAssertions;

using Main.Domain.Aggregates;
using Main.Domain.Constants;
using Main.Domain.Entities;
using Main.Domain.Enums;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Tests.Aggregates;

public sealed class ChatTests
{
    private static readonly DateTimeOffset UtcNow = DateTimeOffset.UtcNow;
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private const string ValidTitle = "My Chat";
    private const string ValidModelId = "claude-3-haiku";
    private static readonly ChatId ValidChatId = ChatId.UnsafeFrom("cht_01JGX12345678901234567890");
    private static readonly ChatId AnotherChatId = ChatId.UnsafeFrom("cht_01JGX09876543210987654321");
    private static readonly MessageId ValidMessageId = MessageId.UnsafeFrom("msg_01JGX123456789012345678901");

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        Outcome<Chat> outcome = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Id.Should().Be(ValidChatId);
        outcome.Value.UserId.Should().Be(ValidUserId);
        outcome.Value.Title.Should().Be(ValidTitle);
        outcome.Value.ModelId.Should().Be(ValidModelId);
        outcome.Value.IsArchived.Should().BeFalse();
        outcome.Value.CreatedAt.Should().Be(UtcNow);
        outcome.Value.UpdatedAt.Should().Be(UtcNow);
        outcome.Value.Messages.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithValidData_ShouldUseProvidedId()
    {
        Outcome<Chat> outcome = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Id.Should().Be(ValidChatId);
        outcome.Value.Id.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldReturnFailure()
    {
        Outcome<Chat> outcome = Chat.Create(ValidChatId, Guid.Empty, ValidTitle, ValidModelId, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.UserIdRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyTitle_ShouldReturnFailure(string? title)
    {
        Outcome<Chat> outcome = Chat.Create(ValidChatId, ValidUserId, title!, ValidModelId, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.TitleRequired);
    }

    [Fact]
    public void Create_WithTooLongTitle_ShouldReturnFailure()
    {
        string title = new('a', ChatConstants.MaxTitleLength + 1);

        Outcome<Chat> outcome = Chat.Create(ValidChatId, ValidUserId, title, ValidModelId, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.TitleTooLong);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyModelId_ShouldReturnFailure(string? modelId)
    {
        Outcome<Chat> outcome = Chat.Create(ValidChatId, ValidUserId, ValidTitle, modelId!, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.ModelIdRequired);
    }

    [Fact]
    public void RenameTitle_WithValidTitle_ShouldUpdateTitle()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        string newTitle = "Updated Chat Title";
        DateTimeOffset updateTime = UtcNow.AddHours(1);

        Outcome outcome = chat.RenameTitle(newTitle, updateTime);

        outcome.IsSuccess.Should().BeTrue();
        chat.Title.Should().Be(newTitle);
        chat.UpdatedAt.Should().Be(updateTime);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void RenameTitle_WithEmptyTitle_ShouldReturnFailure(string? title)
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;

        Outcome outcome = chat.RenameTitle(title!, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.TitleRequired);
    }

    [Fact]
    public void RenameTitle_WithTooLongTitle_ShouldReturnFailure()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        string title = new('a', ChatConstants.MaxTitleLength + 1);

        Outcome outcome = chat.RenameTitle(title, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.TitleTooLong);
    }

    [Fact]
    public void RenameTitle_OnArchivedChat_ShouldReturnFailure()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        chat.Archive(UtcNow);

        Outcome outcome = chat.RenameTitle("New Title", UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.CannotModifyArchivedChat);
    }

    [Fact]
    public void Archive_WhenNotArchived_ShouldReturnSuccessAndArchive()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        DateTimeOffset updateTime = UtcNow.AddHours(1);

        Outcome outcome = chat.Archive(updateTime);

        outcome.IsSuccess.Should().BeTrue();
        chat.IsArchived.Should().BeTrue();
        chat.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldReturnFailure()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        chat.Archive(UtcNow);

        Outcome outcome = chat.Archive(UtcNow.AddHours(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.AlreadyArchived);
    }

    [Fact]
    public void Unarchive_WhenArchived_ShouldReturnSuccessAndUnarchive()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        chat.Archive(UtcNow);
        DateTimeOffset updateTime = UtcNow.AddHours(1);

        Outcome outcome = chat.Unarchive(updateTime);

        outcome.IsSuccess.Should().BeTrue();
        chat.IsArchived.Should().BeFalse();
        chat.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void Unarchive_WhenNotArchived_ShouldReturnFailure()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;

        Outcome outcome = chat.Unarchive(UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.NotArchived);
    }

    [Fact]
    public void AddUserMessage_WithValidContent_ShouldAddMessage()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        string messageContent = "Hello, World!";
        DateTimeOffset updateTime = UtcNow.AddHours(1);

        Outcome<Message> outcome = chat.AddUserMessage(ValidMessageId, messageContent, updateTime);

        outcome.IsSuccess.Should().BeTrue();
        chat.Messages.Should().HaveCount(1);
        chat.Messages.First().MessageContent.Should().Be(messageContent);
        chat.Messages.First().MessageRole.Should().Be(MessageRole.User);
        chat.Messages.First().SequenceNumber.Should().Be(0);
        chat.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void AddUserMessage_MultipleMessages_ShouldAddAll()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;

        chat.AddUserMessage(MessageId.UnsafeFrom("msg_01JGX123456789012345678901"), "First message", UtcNow.AddMinutes(1));
        chat.AddUserMessage(MessageId.UnsafeFrom("msg_01JGX123456789012345678902"), "Second message", UtcNow.AddMinutes(2));
        chat.AddUserMessage(MessageId.UnsafeFrom("msg_01JGX123456789012345678903"), "Third message", UtcNow.AddMinutes(3));

        chat.Messages.Should().HaveCount(3);

        List<Message> messages = chat.Messages.OrderBy(m => m.SequenceNumber).ToList();
        messages[0].SequenceNumber.Should().Be(0);
        messages[1].SequenceNumber.Should().Be(1);
        messages[2].SequenceNumber.Should().Be(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void AddUserMessage_WithEmptyContent_ShouldReturnFailure(string? content)
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;

        Outcome<Message> outcome = chat.AddUserMessage(ValidMessageId, content!, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(MessageFaults.MessageContentRequired);
    }

    [Fact]
    public void AddUserMessage_OnArchivedChat_ShouldReturnFailure()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        chat.Archive(UtcNow);

        Outcome<Message> outcome = chat.AddUserMessage(ValidMessageId, "Hello", UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.CannotModifyArchivedChat);
    }

    [Fact]
    public void AddAssistantMessage_WithValidContent_ShouldAddMessage()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        string messageContent = "I'm here to help!";
        DateTimeOffset updateTime = UtcNow.AddHours(1);

        Outcome<Message> outcome = chat.AddAssistantMessage(ValidMessageId, messageContent, updateTime);

        outcome.IsSuccess.Should().BeTrue();
        chat.Messages.Should().HaveCount(1);
        chat.Messages.First().MessageContent.Should().Be(messageContent);
        chat.Messages.First().MessageRole.Should().Be(MessageRole.Assistant);
        chat.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void AddAssistantMessage_OnArchivedChat_ShouldReturnFailure()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        chat.Archive(UtcNow);

        Outcome<Message> outcome = chat.AddAssistantMessage(ValidMessageId, "Hello", UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.CannotModifyArchivedChat);
    }

    [Fact]
    public void Create_WithDifferentIds_ShouldHaveDifferentIds()
    {
        Chat chat1 = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        Chat chat2 = Chat.Create(AnotherChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;

        chat1.Id.Should().NotBe(chat2.Id);
    }

    #region Pin Tests

    [Fact]
    public void Pin_WhenNotPinned_ShouldReturnSuccessAndPin()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        DateTimeOffset updateTime = UtcNow.AddHours(1);

        Outcome outcome = chat.Pin(updateTime);

        outcome.IsSuccess.Should().BeTrue();
        chat.IsPinned.Should().BeTrue();
        chat.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void Pin_WhenAlreadyPinned_ShouldReturnFailure()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        chat.Pin(UtcNow);

        Outcome outcome = chat.Pin(UtcNow.AddHours(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.AlreadyPinned);
    }

    [Fact]
    public void Pin_WhenArchived_ShouldUnarchiveAndPin()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        chat.Archive(UtcNow);

        Outcome outcome = chat.Pin(UtcNow.AddHours(1));

        outcome.IsSuccess.Should().BeTrue();
        chat.IsPinned.Should().BeTrue();
        chat.IsArchived.Should().BeFalse();
    }

    #endregion

    #region Unpin Tests

    [Fact]
    public void Unpin_WhenPinned_ShouldReturnSuccessAndUnpin()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        chat.Pin(UtcNow);
        DateTimeOffset updateTime = UtcNow.AddHours(1);

        Outcome outcome = chat.Unpin(updateTime);

        outcome.IsSuccess.Should().BeTrue();
        chat.IsPinned.Should().BeFalse();
        chat.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void Unpin_WhenNotPinned_ShouldReturnFailure()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;

        Outcome outcome = chat.Unpin(UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.NotPinned);
    }

    #endregion

    #region Archive and Pin Interaction Tests

    [Fact]
    public void Archive_WhenPinned_ShouldUnpinAndArchive()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        chat.Pin(UtcNow);

        Outcome outcome = chat.Archive(UtcNow.AddHours(1));

        outcome.IsSuccess.Should().BeTrue();
        chat.IsArchived.Should().BeTrue();
        chat.IsPinned.Should().BeFalse();
    }

    #endregion

    #region EditMessageAndRemoveSubsequent Tests

    [Fact]
    public void EditMessageAndRemoveSubsequent_WithValidData_ShouldEditMessageAndRemoveSubsequent()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        MessageId messageId1 = MessageId.UnsafeFrom("msg_01JGX123456789012345678901");
        MessageId messageId2 = MessageId.UnsafeFrom("msg_01JGX123456789012345678902");
        MessageId messageId3 = MessageId.UnsafeFrom("msg_01JGX123456789012345678903");

        chat.AddUserMessage(messageId1, "First message", UtcNow.AddMinutes(1));
        chat.AddAssistantMessage(messageId2, "Second message", UtcNow.AddMinutes(2));
        chat.AddUserMessage(messageId3, "Third message", UtcNow.AddMinutes(3));

        DateTimeOffset editTime = UtcNow.AddHours(1);
        string newContent = "Edited first message";

        Outcome outcome = chat.EditMessageAndRemoveSubsequent(messageId1, newContent, editTime);

        outcome.IsSuccess.Should().BeTrue();
        chat.Messages.Should().HaveCount(1);
        chat.Messages.First().MessageContent.Should().Be(newContent);
        chat.Messages.First().EditedAt.Should().Be(editTime);
        chat.UpdatedAt.Should().Be(editTime);
    }

    [Fact]
    public void EditMessageAndRemoveSubsequent_ShouldKeepMessagesBefore()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        MessageId messageId1 = MessageId.UnsafeFrom("msg_01JGX123456789012345678901");
        MessageId messageId2 = MessageId.UnsafeFrom("msg_01JGX123456789012345678902");
        MessageId messageId3 = MessageId.UnsafeFrom("msg_01JGX123456789012345678903");
        MessageId messageId4 = MessageId.UnsafeFrom("msg_01JGX123456789012345678904");

        chat.AddUserMessage(messageId1, "First message", UtcNow.AddMinutes(1));
        chat.AddAssistantMessage(messageId2, "Second message", UtcNow.AddMinutes(2));
        chat.AddUserMessage(messageId3, "Third message", UtcNow.AddMinutes(3));
        chat.AddAssistantMessage(messageId4, "Fourth message", UtcNow.AddMinutes(4));

        Outcome outcome = chat.EditMessageAndRemoveSubsequent(messageId3, "Edited third", UtcNow.AddHours(1));

        outcome.IsSuccess.Should().BeTrue();
        chat.Messages.Should().HaveCount(3);
        chat.Messages.First().Id.Should().Be(messageId1);
        chat.Messages.Last().Id.Should().Be(messageId3);
        chat.Messages.Last().MessageContent.Should().Be("Edited third");
    }

    [Fact]
    public void EditMessageAndRemoveSubsequent_ShouldUpdateNextSequenceNumber()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        MessageId messageId1 = MessageId.UnsafeFrom("msg_01JGX123456789012345678901");
        MessageId messageId2 = MessageId.UnsafeFrom("msg_01JGX123456789012345678902");
        MessageId messageId3 = MessageId.UnsafeFrom("msg_01JGX123456789012345678903");

        chat.AddUserMessage(messageId1, "First message", UtcNow.AddMinutes(1));
        chat.AddAssistantMessage(messageId2, "Second message", UtcNow.AddMinutes(2));
        chat.AddUserMessage(messageId3, "Third message", UtcNow.AddMinutes(3));

        chat.NextSequenceNumber.Should().Be(3);

        chat.EditMessageAndRemoveSubsequent(messageId1, "Edited first", UtcNow.AddHours(1));

        chat.NextSequenceNumber.Should().Be(1);
    }

    [Fact]
    public void EditMessageAndRemoveSubsequent_OnArchivedChat_ShouldReturnFailure()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        chat.AddUserMessage(ValidMessageId, "Hello", UtcNow.AddMinutes(1));
        chat.Archive(UtcNow.AddMinutes(2));

        Outcome outcome = chat.EditMessageAndRemoveSubsequent(ValidMessageId, "New content", UtcNow.AddHours(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(ChatFaults.CannotModifyArchivedChat);
    }

    [Fact]
    public void EditMessageAndRemoveSubsequent_WithNonExistentMessage_ShouldReturnFailure()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        chat.AddUserMessage(ValidMessageId, "Hello", UtcNow.AddMinutes(1));
        MessageId nonExistentId = MessageId.UnsafeFrom("msg_01JGX000000000000000000000");

        Outcome outcome = chat.EditMessageAndRemoveSubsequent(nonExistentId, "New content", UtcNow.AddHours(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(MessageFaults.MessageNotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void EditMessageAndRemoveSubsequent_WithEmptyContent_ShouldReturnFailure(string? newContent)
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        chat.AddUserMessage(ValidMessageId, "Hello", UtcNow.AddMinutes(1));

        Outcome outcome = chat.EditMessageAndRemoveSubsequent(ValidMessageId, newContent!, UtcNow.AddHours(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(MessageFaults.MessageContentRequired);
    }

    [Fact]
    public void EditMessageAndRemoveSubsequent_LastMessage_ShouldOnlyEditNotRemove()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        MessageId messageId1 = MessageId.UnsafeFrom("msg_01JGX123456789012345678901");
        MessageId messageId2 = MessageId.UnsafeFrom("msg_01JGX123456789012345678902");
        MessageId messageId3 = MessageId.UnsafeFrom("msg_01JGX123456789012345678903");

        chat.AddUserMessage(messageId1, "First message", UtcNow.AddMinutes(1));
        chat.AddAssistantMessage(messageId2, "Second message", UtcNow.AddMinutes(2));
        chat.AddUserMessage(messageId3, "Third message", UtcNow.AddMinutes(3));

        Outcome outcome = chat.EditMessageAndRemoveSubsequent(messageId3, "Edited third", UtcNow.AddHours(1));

        outcome.IsSuccess.Should().BeTrue();
        chat.Messages.Should().HaveCount(3);
        chat.Messages.Last().MessageContent.Should().Be("Edited third");
    }

    [Fact]
    public void EditMessageAndRemoveSubsequent_AssistantMessage_ShouldReturnFailure()
    {
        Chat chat = Chat.Create(ValidChatId, ValidUserId, ValidTitle, ValidModelId, UtcNow).Value;
        MessageId userMsgId = MessageId.UnsafeFrom("msg_01JGX123456789012345678901");
        MessageId assistantMsgId = MessageId.UnsafeFrom("msg_01JGX123456789012345678902");

        chat.AddUserMessage(userMsgId, "User message", UtcNow.AddMinutes(1));
        chat.AddAssistantMessage(assistantMsgId, "Assistant message", UtcNow.AddMinutes(2));

        Outcome outcome = chat.EditMessageAndRemoveSubsequent(assistantMsgId, "Edited assistant", UtcNow.AddHours(1));

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(MessageFaults.MessageEditNotAllowed);
    }

    #endregion
}