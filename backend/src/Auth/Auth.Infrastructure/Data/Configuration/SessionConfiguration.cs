using Auth.Domain.Aggregates;
using Auth.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SharedKernel.Infrastructure.Data;

namespace Auth.Infrastructure.Data.Configuration;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> b)
    {
        b.HasKey(s => s.Id);

        b.Property(s => s.Id)
            .ValueGeneratedNever()
            .HasConversion
            (
                id => id.Value,
                s => SessionId.UnsafeFrom(s)
            )
            .HasColumnType($"{DataConfigurationConstants.DefaultStringColumnType}({SessionId.Length})");

        b.Property(s => s.UserId)
            .IsRequired()
            .HasConversion
            (
                id => id.Value,
                guid => UserId.UnsafeFromGuid(guid)
            )
            .HasColumnType("uuid");

        b.ComplexProperty(s => s.Fingerprint, fp => fp.ConfigureFingerprint());

        b.Property(s => s.RefreshTokenKey)
            .IsRequired()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength)
            .HasColumnType(DataConfigurationConstants.DefaultStringColumnType);

        b.Property(s => s.RefreshTokenHash)
            .IsRequired()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength)
            .HasColumnType(DataConfigurationConstants.DefaultStringColumnType);

        b.Property(s => s.OldRefreshTokenKey)
            .IsRequired(false)
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength)
            .HasColumnType(DataConfigurationConstants.DefaultStringColumnType);

        b.Property(s => s.OldRefreshTokenHash)
            .IsRequired(false)
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength)
            .HasColumnType(DataConfigurationConstants.DefaultStringColumnType);

        b.Property(s => s.CreatedAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(s => s.ExpiresAt)
            .IsRequired()
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(s => s.LastRefreshedAt)
            .IsRequired(false)
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(s => s.RevokeReason)
            .IsRequired(false)
            .HasConversion<string>()
            .HasMaxLength(DataConfigurationConstants.DefaultStringMaxLength);

        b.Property(s => s.RevokedAt)
            .IsRequired(false)
            .HasColumnType(DataConfigurationConstants.DefaultTimeColumnType);

        b.Property(s => s.Version)
            .IsRequired()
            .IsConcurrencyToken();

        b.HasIndex(s => s.UserId);

        b.HasIndex(s => s.RefreshTokenKey)
            .IsUnique();

        b.HasIndex(s => s.OldRefreshTokenKey);

        b.HasIndex(s => s.ExpiresAt);

        b.HasIndex(s => s.RevokedAt);

        b.HasIndex(s => new { s.UserId, s.RevokedAt });

        b.HasIndex(s => new { s.ExpiresAt, s.RevokedAt });
    }
}