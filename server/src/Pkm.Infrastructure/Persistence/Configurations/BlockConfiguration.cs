using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Blocks;
using Pkm.Domain.Pages;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("Blocks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Property(x => x.OrderKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.TextContent)
            .HasMaxLength(20000);

        builder.Property(x => x.PropsJson)
            .HasMaxLength(20000)
            .HasColumnType("jsonb");

        builder.Property(x => x.SchemaVersion)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion(
                v => v.Value,
                v => BlockTypeCode.From(v))
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => new { x.PageId, x.ParentBlockId, x.OrderKey })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.ParentBlockId)
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne<Page>()
            .WithMany()
            .HasForeignKey(x => x.PageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Block>()
            .WithMany()
            .HasForeignKey(x => x.ParentBlockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}