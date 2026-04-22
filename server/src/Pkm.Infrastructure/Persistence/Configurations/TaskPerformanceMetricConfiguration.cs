using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Recommendations;
using Pkm.Domain.Tasks;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class TaskPerformanceMetricConfiguration : IEntityTypeConfiguration<TaskPerformanceMetric>
{
    public void Configure(EntityTypeBuilder<TaskPerformanceMetric> builder)
    {
        builder.ToTable("TaskPerformanceMetrics");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.TaskId, x.UserId })
            .IsUnique()
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