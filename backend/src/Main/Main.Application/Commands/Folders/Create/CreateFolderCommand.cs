using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Folders.Create;

public sealed record CreateFolderCommand(string Name) : ICommand<CreateFolderResponse>;