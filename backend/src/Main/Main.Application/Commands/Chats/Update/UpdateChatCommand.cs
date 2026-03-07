using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Chats.Update;

public sealed record UpdateChatCommand
(
    string ChatId,
    string? NewTitle,
    bool? IsArchived,
    bool? IsPinned,
    string? FolderId,
    bool HasFolderId
) : ICommand<UpdateChatResponse>;