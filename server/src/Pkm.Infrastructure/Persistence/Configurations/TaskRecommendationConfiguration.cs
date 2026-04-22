using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Recommendations;
using Pkm.Domain.Tasks;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class TaskRecommendationConfiguration : IEntityTypeConfiguration<TaskRecommendation>
{
    public void Configure(EntityTypeBuilder<TaskRecommendation> builder)
    {
        builder.ToTable("TaskRecommendations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Score)
            .HasPrecision(5, 2);

        builder.Property(x => x.Reason)
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(x => new { x.UserId, x.WorkspaceId, x.Status, x.Score })
            .IsDescending(false, false, false, true)
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.TaskId)
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkTask>()
            .WithMany()
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}