using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Services;
using Main.Domain.Aggregates;

using Microsoft.EntityFrameworkCore;

namespace Main.Infrastructure.Preferences;

internal sealed class UserPreferenceResolver(IMainDbContext dbContext) : IUserPreferenceResolver
{
    public async Task<bool> IsMemoryEnabledAsync(Guid userId, CancellationToken cancellationToken)
    {
        Preference? preference = await dbContext.Preferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        return preference?.MemoryEnabled ?? true;
    }
}