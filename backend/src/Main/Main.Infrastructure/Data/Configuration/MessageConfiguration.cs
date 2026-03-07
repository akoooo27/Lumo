using Main.Domain.Entities;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SharedKernel.Infrastructure.Data;

namespace Main.Infrastructure.Data.Configuration;

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.HasKey(m => m.Id);

        b.Property(m => m.Id)
            .ValueGeneratedNever()
            .HasConversion
            (
                id => id.Value,
                s => MessageId.UnsafeFrom(s)
            )
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({MessageId.Length})");

        b.Property(m => m.ChatId)
            .ValueGeneratedNever()
            .HasConversion
            (
                id => id.Value,
                s => ChatId.UnsafeFrom(s)
            )
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({ChatId.Length})");

        b.Property(m => m.MessageRole)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(m => m.MessageContent)
            .IsRequired()
            .HasColumnType("text");

        b.Property(m => m.InputTokenCount)
            .IsRequired(false)
            .HasColumnType("bigint");

        b.Property(m => m.OutputTokenCount)
            .IsRequired(false)
            .HasColumnType("bigint");

        b.Property(m => m.TotalTokenCount)
            .IsRequired(false)
            .HasColumnType("bigint");

        b.Property(m => m.SequenceNumber)
            .IsRequired()
            .HasColumnType("integer");

        b.Property(m => m.CreatedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(m => m.EditedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.HasIndex(m => m.ChatId);

        b.HasIndex(m => new { m.ChatId, m.SequenceNumber })
            .IsUnique();
    }
}