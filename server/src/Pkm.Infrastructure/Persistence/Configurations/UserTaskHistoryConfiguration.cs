using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pkm.Domain.Recommendations;
using Pkm.Domain.Tasks;

namespace Pkm.Infrastructure.Persistence.Configurations;

internal sealed class UserTaskHistoryConfiguration : IEntityTypeConfiguration<UserTaskHistory>
{
    public void Configure(EntityTypeBuilder<UserTaskHistory> builder)
    {
        builder.ToTable("UserTaskHistories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.TaskId)
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.UserId)
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne<WorkTask>()
            .WithMany()
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}