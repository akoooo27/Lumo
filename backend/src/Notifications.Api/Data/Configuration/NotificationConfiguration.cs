using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Notifications.Api.Data.Entities;

using SharedKernel.Infrastructure.Data;

namespace Notifications.Api.Data.Configuration;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.HasKey(n => n.Id);

        b.Property(n => n.Id)
            .ValueGeneratedNever();

        b.Property(n => n.UserId)
            .IsRequired();

        b.Property(n => n.Identifier)
            .IsRequired();

        b.Property(n => n.Category)
            .IsRequired()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(n => n.BodyPreview)
            .IsRequired()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(n => n.SourceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(n => n.SourceId)
            .IsRequired()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(n => n.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(n => n.EmailStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(n => n.CreatedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(n => n.ReadAt)
            .IsRequired(false)
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.HasIndex(n => n.Identifier)
            .IsUnique();

        b.HasIndex(n => new { n.UserId, n.CreatedAt });
    }
}