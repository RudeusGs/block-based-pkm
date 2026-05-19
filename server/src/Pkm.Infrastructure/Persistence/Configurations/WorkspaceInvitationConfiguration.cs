using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class WorkspaceInvitationConfiguration : IEntityTypeConfiguration<WorkspaceInvitation>
{
    public void Configure(EntityTypeBuilder<WorkspaceInvitation> builder)
    {
        builder.ToTable("WorkspaceInvitations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(WorkspaceInvitation.MaxEmailLength);

        builder.Property(x => x.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(WorkspaceInvitation.MaxEmailLength);

        builder.Property(x => x.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(WorkspaceInvitation.MaxTokenHashLength);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.WorkspaceId, x.NormalizedEmail, x.AcceptedAtUtc, x.ExpiresAtUtc })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.InvitedByUserId, x.CreatedDate })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
