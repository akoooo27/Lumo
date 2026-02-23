using TickerQ.Utilities.Base;

namespace Main.Infrastructure.Jobs;

public class CronJobs(ICronJobHelper cronJobHelper)
{
    [TickerFunction("PurgeDeletedMemoriesJob", "0 0 0 * * *")]
    public async Task PurgeDeletedMemoriesAsync(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        await cronJobHelper.PurgeDeletedMemoriesAsync(cancellationToken);
    }
}
