using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Infrastructure.Persistence.Outbox;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SchemaVersion)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.PayloadJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.TraceId)
            .HasMaxLength(128);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.LastError)
            .HasMaxLength(4000);

        builder.Property(x => x.LockId)
            .HasMaxLength(128);

        builder.HasIndex(x => new { x.Status, x.OccurredAtUtc })
            .IsDescending(false, false);

        builder.HasIndex(x => new { x.EventType, x.OccurredAtUtc })
            .IsDescending(false, true);

        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.LockId);
    }
}
