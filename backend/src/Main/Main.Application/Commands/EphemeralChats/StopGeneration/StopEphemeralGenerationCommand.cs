using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.EphemeralChats.StopGeneration;

public sealed record StopEphemeralGenerationCommand(string EphemeralChatId, string StreamId) : ICommand;