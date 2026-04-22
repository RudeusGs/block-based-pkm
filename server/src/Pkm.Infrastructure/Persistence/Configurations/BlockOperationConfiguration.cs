using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Blocks;
using Pkm.Domain.Pages;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class BlockOperationConfiguration : IEntityTypeConfiguration<BlockOperation>
{
    public void Configure(EntityTypeBuilder<BlockOperation> builder)
    {
        builder.ToTable("BlockOperations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PayloadJson)
            .HasMaxLength(10000)
            .HasColumnType("jsonb");

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.Property(x => x.BaseRevision)
            .IsRequired();

        builder.Property(x => x.AppliedRevision)
            .IsRequired();

        // Guards against duplicate applied revisions per page.
        // This index is necessary but not sufficient for full end-to-end concurrency control.
        builder.HasIndex(x => new { x.PageId, x.AppliedRevision })
            .IsUnique();

        builder.HasIndex(x => new { x.PageId, x.CreatedDate });

        builder.HasOne<Page>()
            .WithMany()
            .HasForeignKey(x => x.PageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
