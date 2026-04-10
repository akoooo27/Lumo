namespace Main.Application.Abstractions.Stream;

public interface IChatLockService
{
    Task<bool> TryAcquireLockAsync(string chatId, string ownerId, CancellationToken cancellationToken);

    Task ReleaseLockAsync(string chatId, string ownerId, CancellationToken cancellationToken);

    Task<bool> IsGeneratingAsync(string chatId, CancellationToken cancellationToken);

    Task RequestCancellationAsync(string streamId, CancellationToken cancellationToken);

    Task<bool> IsCancellationRequestedAsync(string streamId, CancellationToken cancellationToken);

    Task ClearCancellationAsync(string streamId, CancellationToken cancellationToken);
}