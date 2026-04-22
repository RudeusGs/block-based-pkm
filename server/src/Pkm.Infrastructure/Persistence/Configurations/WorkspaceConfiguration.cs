using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => x.OwnerId)
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => new { x.OwnerId, x.CreatedDate })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.CreatedDate)
            .HasFilter("\"IsDeleted\" = false");
    }
}