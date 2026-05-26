using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Messaging;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class MessagePinConfiguration : IEntityTypeConfiguration<MessagePin>
{
    public void Configure(EntityTypeBuilder<MessagePin> builder)
    {
        builder.ToTable("MessagePins");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ConversationId, x.MessageId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.ConversationId, x.CreatedDate })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

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
