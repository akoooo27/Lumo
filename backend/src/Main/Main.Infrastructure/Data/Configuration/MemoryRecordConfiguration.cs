using Main.Application.Abstractions.Memory;
using Main.Infrastructure.Data.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SharedKernel.Infrastructure.Data;

namespace Main.Infrastructure.Data.Configuration;

internal sealed class MemoryRecordConfiguration : IEntityTypeConfiguration<MemoryRecord>
{
    private const int EmbeddingDimensions = 1536;

    public void Configure(EntityTypeBuilder<MemoryRecord> b)
    {
        b.HasKey(m => m.Id);

        b.Property(m => m.Id)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}(30)");

        b.Property(m => m.UserId)
            .IsRequired()
            .HasColumnType("uuid");

        b.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(MemoryConstants.MaxContentLength)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({MemoryConstants.MaxContentLength})");

        b.Property(m => m.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}(20)");

        b.Property(m => m.Embedding)
            .IsRequired()
            .HasColumnType($"vector({EmbeddingDimensions})");

        b.Property(m => m.CreatedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(m => m.UpdatedAt)
            .IsRequired(false)
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(m => m.LastAccessedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(m => m.AccessCount)
            .IsRequired()
            .HasDefaultValue(0);

        b.Property(m => m.Importance)
            .IsRequired()
            .HasDefaultValue(MemoryConstants.DefaultImportance);

        b.Property(m => m.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        b.HasIndex(m => m.UserId);
        b.HasIndex(m => new { m.UserId, m.IsActive, m.CreatedAt });

        b.HasIndex(m => m.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");
    }
}