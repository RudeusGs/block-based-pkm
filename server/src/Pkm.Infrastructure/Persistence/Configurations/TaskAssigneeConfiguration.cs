using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Tasks;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class TaskAssigneeConfiguration : IEntityTypeConfiguration<TaskAssignee>
{
    public void Configure(EntityTypeBuilder<TaskAssignee> builder)
    {
        builder.ToTable("TaskAssignees");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.TaskId, x.UserId })
            .IsUnique();

        builder.HasIndex(x => new { x.UserId, x.TaskId });

        builder.HasOne<WorkTask>()
            .WithMany()
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}