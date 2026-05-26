using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Pages;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class PageRecentConfiguration : IEntityTypeConfiguration<PageRecent>
{
    public void Configure(EntityTypeBuilder<PageRecent> builder)
    {
        builder.ToTable("PageRecents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LastVisitedAtUtc)
            .IsRequired();

        builder.Property(x => x.VisitCount)
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.PageId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.UserId, x.LastVisitedAtUtc })
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
