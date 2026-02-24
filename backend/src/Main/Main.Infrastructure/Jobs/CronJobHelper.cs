using Main.Application.Abstractions.Memory;
using Main.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

using SharedKernel;

namespace Main.Infrastructure.Jobs;

internal sealed class CronJobHelper(
    MainDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICronJobHelper
{
    public async Task PurgeDeletedMemoriesAsync(CancellationToken cancellationToken = default)
    {
        const int batchSize = 1000;
        DateTimeOffset cutoff = dateTimeProvider.UtcNow.AddDays(-MemoryConstants.StaleDaysThreshold);

        while (true)
        {
            List<string> idsToDelete = await dbContext.Memories
                .Where(m => !m.IsActive && m.UpdatedAt.HasValue && m.UpdatedAt.Value <= cutoff)
                .OrderBy(m => m.UpdatedAt)
                .Select(m => m.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (idsToDelete.Count == 0)
                return;

            await dbContext.Memories
                .Where(m => idsToDelete.Contains(m.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}