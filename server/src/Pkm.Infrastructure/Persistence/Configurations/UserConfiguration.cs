using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", table =>
        {
            table.HasCheckConstraint("CK_User_Email_Format", "\"Email\" LIKE '%_@__%.__%'");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName)
            .IsRequired()
            .HasMaxLength(User.MaxNameLength);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(User.MaxEmailLength);

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(User.MaxNameLength);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(User.MaxUrlLength);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(User.MaxStatusLength);

        builder.Property(x => x.PasswordHash)
            .IsRequired();

        builder.HasIndex(x => x.UserName)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}