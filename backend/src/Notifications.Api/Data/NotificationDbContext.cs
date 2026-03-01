using MassTransit;

using Microsoft.EntityFrameworkCore;

using Notifications.Api.ReadModels;

namespace Notifications.Api.Data;

internal sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options)
    : DbContext(options), INotificationDbContext
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
    }
}