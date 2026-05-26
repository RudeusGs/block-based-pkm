using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Messaging;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("MessageReactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Emoji)
            .IsRequired()
            .HasMaxLength(MessageReaction.MaxEmojiLength);

        builder.HasIndex(x => new { x.MessageId, x.UserId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.MessageId, x.Emoji })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
