using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Pages;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class PageFavoriteConfiguration : IEntityTypeConfiguration<PageFavorite>
{
    public void Configure(EntityTypeBuilder<PageFavorite> builder)
    {
        builder.ToTable("PageFavorites");
        builder.HasKey(x => x.Id);

        builder.Ignore(x => x.RowVersion);

        builder.HasIndex(x => new { x.UserId, x.PageId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.UserId, x.CreatedDate })
            .IsDescending(false, true)
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Page>()
            .WithMany()
            .HasForeignKey(x => x.PageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
