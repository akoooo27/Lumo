using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Folders.GetFolders;

public sealed record GetFoldersQuery : IQuery<GetFoldersResponse>;