using Main.Domain.Constants;
using Main.Domain.Entities;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SharedKernel.Infrastructure.Data;

namespace Main.Infrastructure.Data.Configuration;

internal sealed class WorkflowRunConfiguration : IEntityTypeConfiguration<WorkflowRun>
{
    public void Configure(EntityTypeBuilder<WorkflowRun> b)
    {
        b.HasKey(r => r.Id);

        b.Property(r => r.Id)
            .ValueGeneratedNever()
            .HasConversion
            (
                id => id.Value,
                s => WorkflowRunId.UnsafeFrom(s)
            )
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({WorkflowRunId.Length})");

        b.Property(r => r.WorkflowId)
            .HasConversion
            (
                id => id.Value,
                s => WorkflowId.UnsafeFrom(s)
            )
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({WorkflowId.Length})");

        b.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(r => r.ScheduledFor)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(r => r.StartedAt)
            .IsRequired(false)
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(r => r.CompletedAt)
            .IsRequired(false)
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(r => r.ResultMarkdown)
            .IsRequired(false)
            .HasColumnType("text");

        b.Property(r => r.FailureMessage)
            .IsRequired(false)
            .HasColumnType("text");

        b.Property(r => r.SkipReason)
            .IsRequired(false)
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(r => r.ModelIdUsed)
            .IsRequired()
            .HasMaxLength(WorkflowConstants.MaxModelIdLength)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({WorkflowConstants.MaxModelIdLength})");

        b.Property(r => r.UseWebSearchUsed)
            .IsRequired()
            .HasColumnType("boolean");

        b.Property(r => r.InstructionSnapshot)
            .IsRequired()
            .HasColumnType("text");

        b.Property(r => r.TitleSnapshot)
            .IsRequired()
            .HasMaxLength(WorkflowConstants.MaxTitleLength)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({WorkflowConstants.MaxTitleLength})");

        b.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.HasIndex(r => new { r.WorkflowId, r.ScheduledFor })
            .IsUnique();

        b.HasIndex(r => r.WorkflowId);
    }
}