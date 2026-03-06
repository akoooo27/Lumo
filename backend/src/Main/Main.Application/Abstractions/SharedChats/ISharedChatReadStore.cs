using Main.Application.Queries.SharedChats.GetSharedChat;

namespace Main.Application.Abstractions.SharedChats;

public interface ISharedChatReadStore
{
    Task<GetSharedChatResponse?> GetAsync(string sharedChatId, CancellationToken cancellationToken);

    Task InvalidateCacheAsync(string sharedChatId, CancellationToken cancellationToken);
}