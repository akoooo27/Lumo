using SharedKernel;

namespace Main.Domain.Faults;

public static class MessageFaults
{
    public static readonly Fault MessageIdRequired = Fault.Validation
    (
        title: "Message.MessageIdRequired",
        detail: "MessageId is required."
    );

    public static readonly Fault ChatIdRequired = Fault.Validation
    (
        title: "Message.ChatIdRequired",
        detail: "A chat ID is required to create a message."
    );

    public static readonly Fault InvalidMessageRole = Fault.Validation
    (
        title: "Message.InvalidMessageRole",
        detail: "The message role provided is invalid."
    );

    public static readonly Fault MessageContentRequired = Fault.Validation
    (
        title: "Message.MessageContentRequired",
        detail: "Message content is required and cannot be empty."
    );

    public static readonly Fault NegativeTokenCount = Fault.Validation
    (
        title: "Message.NegativeTokenCount",
        detail: "The token count cannot be negative."
    );

    public static readonly Fault InvalidSequenceNumber = Fault.Validation
    (
        title: "Message.InvalidSequenceNumber",
        detail: "The sequence number must be a non-negative integer."
    );

    public static readonly Fault MessageNotFound = Fault.NotFound
    (
        title: "Message.MessageNotFound",
        detail: "The specified message was not found."
    );

    public static readonly Fault MessageEditNotAllowed = Fault.Validation
    (
        title: "Message.MessageEditNotAllowed",
        detail: "Editing this message is not allowed."
    );

    public static readonly Fault SourcesRequired = Fault.Validation
    (
        title: "Message.SourcesRequired",
        detail: "At least one source must be provided when setting sources for a message."
    );

    public static readonly Fault MessageSourceNotAllowed = Fault.Validation
    (
        title: "Message.MessageSourceNotAllowed",
        detail: "Setting a source is only allowed for assistant messages."
    );
}