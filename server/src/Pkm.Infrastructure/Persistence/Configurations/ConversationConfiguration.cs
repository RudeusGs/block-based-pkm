using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Users;
using Pkm.Domain.Messaging;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LastMessagePreview)
            .HasMaxLength(Conversation.MaxPreviewLength);

        builder.HasIndex(x => new { x.FirstUserId, x.SecondUserId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.FirstUserId, x.LastMessageAtUtc })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.SecondUserId, x.LastMessageAtUtc })
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
