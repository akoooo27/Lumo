using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Memories.DeleteSingle;

public sealed record DeleteMemoryCommand(string MemoryId) : ICommand;