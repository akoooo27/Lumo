using Main.Domain.Aggregates;
using Main.Domain.Constants;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SharedKernel.Infrastructure.Data;

namespace Main.Infrastructure.Data.Configuration;

internal sealed class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> b)
    {
        b.HasKey(w => w.Id);

        b.Property(w => w.Id)
            .ValueGeneratedNever()
            .HasConversion
            (
                id => id.Value,
                s => WorkflowId.UnsafeFrom(s)
            )
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({WorkflowId.Length})");

        b.Property(w => w.UserId)
            .IsRequired()
            .HasColumnType("uuid");

        b.Property(w => w.Title)
            .IsRequired()
            .HasMaxLength(WorkflowConstants.MaxTitleLength)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({WorkflowConstants.MaxTitleLength})");

        b.Property(w => w.Instruction)
            .IsRequired()
            .HasColumnType("text");

        b.Property(w => w.NormalizedInstruction)
            .IsRequired()
            .HasColumnType("text");

        b.Property(w => w.ModelId)
            .IsRequired()
            .HasMaxLength(WorkflowConstants.MaxModelIdLength)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({WorkflowConstants.MaxModelIdLength})");

        b.Property(w => w.UseWebSearch)
            .IsRequired()
            .HasColumnType("boolean");

        b.Property(w => w.DeliveryPolicy)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(w => w.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(w => w.PauseReason)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(w => w.RecurrenceKind)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(w => w.DaysOfWeekMask)
            .IsRequired()
            .HasColumnType("integer");

        b.Property(w => w.LocalTime)
            .IsRequired()
            .HasMaxLength(WorkflowConstants.MaxLocalTimeLength)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({WorkflowConstants.MaxLocalTimeLength})");

        b.Property(w => w.TimeZoneId)
            .IsRequired()
            .HasMaxLength(WorkflowConstants.MaxTimeZoneIdLength)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({WorkflowConstants.MaxTimeZoneIdLength})");

        b.Property(w => w.NextRunAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(w => w.LastRunAt)
            .IsRequired(false)
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(w => w.ConsecutiveFailureCount)
            .IsRequired()
            .HasColumnType("integer");

        b.Property(w => w.DispatchLeaseId)
            .IsRequired(false)
            .HasColumnType("uuid");

        b.Property(w => w.DispatchLeaseUntilUtc)
            .IsRequired(false)
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(w => w.CreatedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(w => w.UpdatedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.HasMany(w => w.WorkflowRuns)
            .WithOne()
            .HasForeignKey(r => r.WorkflowId)
            .HasPrincipalKey(w => w.Id)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(w => new { w.Status, w.NextRunAt, w.DispatchLeaseUntilUtc });

        b.HasIndex(w => new { w.UserId, w.Status });

        b.HasIndex(w => new
        {
            w.UserId,
            w.NormalizedInstruction,
            w.RecurrenceKind,
            w.DaysOfWeekMask,
            w.LocalTime,
            w.TimeZoneId
        })
        .IsUnique()
        .HasFilter("status = 'Active'");
    }
}