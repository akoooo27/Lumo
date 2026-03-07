using Main.Domain.Aggregates;
using Main.Domain.Constants;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SharedKernel.Infrastructure.Data;

namespace Main.Infrastructure.Data.Configuration;

internal sealed class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> b)
    {
        b.HasKey(f => f.Id);

        b.Property(f => f.Id)
            .ValueGeneratedNever()
            .HasConversion
            (
                id => id.Value,
                s => FolderId.UnsafeFrom(s)
            )
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({FolderId.Length})");

        b.Property(f => f.UserId)
            .IsRequired()
            .HasColumnType("uuid");

        b.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(FolderConstants.MaxNameLength)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({FolderConstants.MaxNameLength})");

        b.Property(f => f.NormalizedName)
            .IsRequired()
            .HasMaxLength(FolderConstants.MaxNameLength)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({FolderConstants.MaxNameLength})");

        b.Property(f => f.SortOrder)
            .IsRequired()
            .HasColumnType("integer");

        b.Property(f => f.CreatedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(f => f.UpdatedAt)
            .IsRequired(false)
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.HasIndex(f => new { f.UserId, f.NormalizedName }).IsUnique();
        b.HasIndex(f => new { f.UserId, f.SortOrder });
    }
}