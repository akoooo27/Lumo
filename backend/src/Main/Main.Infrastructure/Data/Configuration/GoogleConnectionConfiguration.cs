using Main.Domain.Aggregates;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SharedKernel.Infrastructure.Data;

namespace Main.Infrastructure.Data.Configuration;

internal sealed class GoogleConnectionConfiguration : IEntityTypeConfiguration<GoogleConnection>
{
    public void Configure(EntityTypeBuilder<GoogleConnection> b)
    {
        b.HasKey(gc => gc.Id);

        b.Property(gc => gc.Id)
            .ValueGeneratedNever()
            .HasConversion
            (
                id => id.Value,
                s => GoogleConnectionId.UnsafeFrom(s)
            )
            .HasColumnType("uuid");

        b.Property(gc => gc.UserId)
            .IsRequired()
            .HasColumnType("uuid");

        b.Property(gc => gc.GoogleEmail)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultStringColumnType);

        b.Property(gc => gc.ProtectedAccessToken)
            .IsRequired()
            .HasColumnType("text");

        b.Property(gc => gc.ProtectedRefreshToken)
            .IsRequired()
            .HasColumnType("text");

        b.Property(gc => gc.CreatedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(gc => gc.UpdatedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(gc => gc.ExpiresAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.HasIndex(gc => gc.UserId)
            .IsUnique();
    }
}