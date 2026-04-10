using SharedKernel;

namespace Main.Application.Faults;

internal static class EphemeralChatOperationFaults
{
    internal static readonly Fault NotFound = Fault.NotFound
    (
        title: "EphemeralChat.NotFound",
        detail: "The specified ephemeral chat was not found."
    );

    internal static readonly Fault GenerationInProgress = Fault.Conflict
    (
        title: "EphemeralChat.GenerationInProgress",
        detail: "The ephemeral chat is currently generating a response. Please wait until the generation is complete."
    );

    internal static readonly Fault NotGenerating = Fault.Conflict
    (
        title: "EphemeralChat.NotGenerating",
        detail: "The ephemeral chat is not currently generating a response."
    );
}