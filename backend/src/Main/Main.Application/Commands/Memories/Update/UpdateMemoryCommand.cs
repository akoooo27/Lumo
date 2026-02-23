using Main.Application.Abstractions.Memory;

using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Memories.Update;

public sealed record UpdateMemoryCommand
(
    string MemoryId,
    string? Content,
    MemoryCategory? Category,
    int? Importance
) : ICommand<UpdateMemoryResponse>;