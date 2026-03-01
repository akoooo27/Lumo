using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Notifications.Api.ReadModels;

namespace Notifications.Api.Data;

internal interface INotificationDbContext
{
    DatabaseFacade Database { get; }

    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}