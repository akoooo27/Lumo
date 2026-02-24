namespace Main.Infrastructure.Jobs;

public interface ICronJobHelper
{
    Task PurgeDeletedMemoriesAsync(CancellationToken cancellationToken = default);
}