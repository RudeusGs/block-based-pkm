using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Users;
using Pkm.Domain.Social;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("Friendships");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.FirstUserId, x.SecondUserId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.FirstUserId, x.CreatedDate })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.SecondUserId, x.CreatedDate })
            .HasFilter("\"IsDeleted\" = false");


        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.FirstUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.SecondUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
