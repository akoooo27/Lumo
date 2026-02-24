using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Preferences.AddInstruction;

public record AddInstructionCommand(string Content) : ICommand<AddInstructionResponse>, ISensitiveRequest;