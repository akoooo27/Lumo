using Main.Domain.Aggregates;
using Main.Domain.Entities;
using Main.Domain.ReadModels;

using Microsoft.EntityFrameworkCore;

namespace Main.Application.Abstractions.Data;

public interface IMainDbContext
{
    DbSet<User> Users { get; }

    DbSet<Chat> Chats { get; }
    DbSet<Message> Messages { get; }

    DbSet<Preference> Preferences { get; }
    DbSet<Instruction> Instructions { get; }
    DbSet<FavoriteModel> FavoriteModels { get; }

    DbSet<SharedChat> SharedChats { get; }

    DbSet<Workflow> Workflows { get; }
    DbSet<WorkflowRun> WorkflowRuns { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}