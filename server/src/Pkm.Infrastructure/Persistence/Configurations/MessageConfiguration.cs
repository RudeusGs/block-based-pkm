using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Files;
using Pkm.Domain.Messaging;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Body)
            .HasMaxLength(Message.MaxBodyLength);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(Message.MaxUrlLength);

        builder.HasIndex(x => new { x.ConversationId, x.CreatedDate })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.RecipientUserId, x.ReadAtUtc, x.CreatedDate })
            .HasFilter("\"IsDeleted\" = false AND \"IsDeletedForEveryone\" = false");

        builder.HasIndex(x => new { x.SenderUserId, x.CreatedDate })
            .HasFilter("\"IsDeleted\" = false");


        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(x => x.AttachmentFileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
