using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Folders.Delete;

public sealed record DeleteFolderCommand(string FolderId) : ICommand;