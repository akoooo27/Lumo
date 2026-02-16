using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Preferences.UpdateInstruction;

public sealed record UpdateInstructionCommand
(
    string InstructionId,
    string NewContent
) : ICommand<UpdateInstructionResponse>, ISensitiveRequest;