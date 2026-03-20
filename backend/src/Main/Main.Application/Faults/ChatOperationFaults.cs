using SharedKernel;

namespace Main.Application.Faults;

internal static class ChatOperationFaults
{
    internal static readonly Fault NotFound = Fault.NotFound
    (
        title: "Chat.NotFound",
        detail: "The specified chat was not found."
    );

    internal static readonly Fault InvalidModel = Fault.Validation
    (
        title: "Chat.InvalidModel",
        detail: "The specified model is not available."
    );

    internal static readonly Fault GenerationInProgress = Fault.Conflict
    (
        title: "Chat.GenerationInProgress",
        detail:
        "Cannot send a new message while AI is generating a response. Please wait for the current response to complete."
    );

    internal static readonly Fault AttachmentsNotSupported = Fault.Validation
    (
        title: "Chat.AttachmentsNotSupported",
        detail: "The specified model does not support attachments."
    );
}