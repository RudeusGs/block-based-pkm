using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Pages;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("Pages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Icon)
            .HasMaxLength(500);

        builder.Property(x => x.CoverImage)
            .HasMaxLength(2048);

        builder.Property(x => x.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Property(x => x.CurrentRevision)
            .IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.ParentPageId, x.IsArchived })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.WorkspaceId, x.CreatedDate })
            .IsDescending(false, true)
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Page>()
            .WithMany()
            .HasForeignKey(x => x.ParentPageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}