using Main.Domain.ValueObjects;

namespace Main.Application.Abstractions.Generators;

public interface IIdGenerator
{
    ChatId NewChatId();

    MessageId NewMessageId();

    StreamId NewStreamId();

    PreferenceId NewPreferenceId();

    InstructionId NewInstructionId();

    SharedChatId NewSharedChatId();

    EphemeralChatId NewEphemeralChatId();

    FavoriteModelId NewFavoriteModelId();

    WorkflowId NewWorkflowId();

    WorkflowRunId NewWorkflowRunId();
}