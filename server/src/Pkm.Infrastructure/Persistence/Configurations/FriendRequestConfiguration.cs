using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Users;
using Pkm.Domain.Social;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.ToTable("FriendRequests");
        builder.HasKey(x => x.Id);

        builder.Ignore(x => x.RowVersion);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(x => new { x.RequesterId, x.AddresseeId, x.Status })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.AddresseeId, x.Status, x.CreatedDate })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.RequesterId, x.Status, x.CreatedDate })
            .HasFilter("\"IsDeleted\" = false");


        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AddresseeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
