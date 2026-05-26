using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Users;
using Pkm.Domain.Profiles;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class UserProfilePageConfiguration : IEntityTypeConfiguration<UserProfilePage>
{
    public void Configure(EntityTypeBuilder<UserProfilePage> builder)
    {
        builder.ToTable("UserProfilePages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Bio)
            .HasMaxLength(UserProfilePage.MaxBioLength);

        builder.Property(x => x.CoverImageUrl)
            .HasMaxLength(UserProfilePage.MaxUrlLength);

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");


        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
