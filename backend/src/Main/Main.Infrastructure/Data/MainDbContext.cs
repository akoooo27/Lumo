using Main.Application.Abstractions.Data;
using Main.Domain.Aggregates;
using Main.Domain.Entities;
using Main.Domain.ReadModels;
using Main.Infrastructure.Data.Entities;

using MassTransit;

using Microsoft.EntityFrameworkCore;

namespace Main.Infrastructure.Data;

internal sealed class MainDbContext(DbContextOptions<MainDbContext> options) : DbContext(options), IMainDbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<Chat> Chats { get; set; }
    public DbSet<Message> Messages { get; set; }

    internal DbSet<MemoryRecord> Memories { get; set; }

    public DbSet<Preference> Preferences { get; set; }
    public DbSet<Instruction> Instructions { get; set; }
    public DbSet<FavoriteModel> FavoriteModels { get; set; }

    public DbSet<SharedChat> SharedChats { get; set; }

    public DbSet<Workflow> Workflows { get; set; }
    public DbSet<WorkflowRun> WorkflowRuns { get; set; }

    public DbSet<Folder> Folders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MainDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
    }
}