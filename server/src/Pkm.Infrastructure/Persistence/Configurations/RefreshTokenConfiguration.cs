using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(RefreshToken.MaxTokenHashLength);

        builder.Property(x => x.ReplacedByTokenHash)
            .HasMaxLength(RefreshToken.MaxTokenHashLength);

        builder.Property(x => x.CreatedByIp)
            .HasMaxLength(RefreshToken.MaxIpAddressLength);

        builder.Property(x => x.RevokedByIp)
            .HasMaxLength(RefreshToken.MaxIpAddressLength);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.UserId, x.ExpiresAtUtc, x.RevokedAtUtc })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}