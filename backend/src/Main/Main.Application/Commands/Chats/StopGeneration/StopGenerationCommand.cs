using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Chats.StopGeneration;

public sealed record StopGenerationCommand(string ChatId, string StreamId) : ICommand;