using Main.Domain.Aggregates;
using Main.Domain.Constants;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NpgsqlTypes;

using SharedKernel.Infrastructure.Data;

namespace Main.Infrastructure.Data.Configuration;

internal sealed class ChatConfiguration : IEntityTypeConfiguration<Chat>
{
    public void Configure(EntityTypeBuilder<Chat> b)
    {
        b.HasKey(c => c.Id);

        b.Property(c => c.Id)
            .ValueGeneratedNever()
            .HasConversion
            (
                id => id.Value,
                s => ChatId.UnsafeFrom(s)
            )
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({ChatId.Length})");

        b.Property(c => c.UserId)
            .IsRequired()
            .HasColumnType("uuid");

        b.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(ChatConstants.MaxTitleLength)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({ChatConstants.MaxTitleLength})");

        b.Property(c => c.ModelId)
            .IsRequired(false)
            .HasMaxLength(ChatConstants.MaxModelIdLength)
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({ChatConstants.MaxModelIdLength})");

        b.Property(c => c.IsArchived)
            .IsRequired()
            .HasColumnType("boolean");

        b.Property(c => c.FolderId)
            .IsRequired(false)
            .HasConversion
            (
                id => id.HasValue ? id.Value.Value : null,
                s => s != null ? FolderId.UnsafeFrom(s) : null
            )
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({FolderId.Length})");

        b.Property(c => c.IsPinned)
            .IsRequired()
            .HasColumnType("boolean");

        b.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(c => c.UpdatedAt)
            .IsRequired(false)
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.HasMany(c => c.Messages)
            .WithOne()
            .HasForeignKey(m => m.ChatId)
            .HasPrincipalKey(c => c.Id)
            .OnDelete(DeleteBehavior.Cascade);

        b.Property<NpgsqlTsVector>("TitleSearchVector")
            .HasColumnType("tsvector")
            .HasComputedColumnSql("to_tsvector('english', title)", stored: true);

        b.HasIndex("TitleSearchVector")
            .HasMethod("GIN");

        b.HasIndex(c => new { c.UserId, c.IsArchived, c.UpdatedAt });
        b.HasIndex(c => new { c.UserId, c.FolderId, c.UpdatedAt });
    }
}