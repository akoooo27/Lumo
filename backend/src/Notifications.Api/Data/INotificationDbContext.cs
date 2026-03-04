using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Notifications.Api.Data.Entities;
using Notifications.Api.ReadModels;

namespace Notifications.Api.Data;

internal interface INotificationDbContext
{
    DatabaseFacade Database { get; }

    DbSet<User> Users { get; }

    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}