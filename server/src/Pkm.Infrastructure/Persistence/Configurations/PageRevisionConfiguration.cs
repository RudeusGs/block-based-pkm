using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Pages;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class PageRevisionConfiguration : IEntityTypeConfiguration<PageRevision>
{
    public void Configure(EntityTypeBuilder<PageRevision> builder)
    {
        builder.ToTable("PageRevisions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.PageId, x.RevisionNumber })
            .IsUnique();

        builder.HasIndex(x => new { x.PageId, x.CreatedDate });

        builder.HasOne<Page>()
            .WithMany()
            .HasForeignKey(x => x.PageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}