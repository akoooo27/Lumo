using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Memories.Import;

public sealed record ImportMemoriesCommand(string Content) : ICommand<ImportMemoriesResponse>;