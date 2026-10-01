using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigloc.Domain.Entities;

namespace Sigloc.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired();

        // Persist the enums as their string names for readability.
        builder.Property(u => u.ProfileType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.AuthProvider)
            .HasConversion<string>()
            .IsRequired();

        // Email is the login credential and must be globally unique.
        builder.HasIndex(u => u.Email).IsUnique();

        // Google account id is unique when present.
        builder.HasIndex(u => u.GoogleId)
            .IsUnique()
            .HasFilter("\"GoogleId\" IS NOT NULL");
    }
}

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.HasKey(token => token.Id);
        builder.Property(token => token.TokenHash).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.UserId);
    }
}
