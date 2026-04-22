using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pkm.Domain.Audit;
using Pkm.Domain.Blocks;
using Pkm.Domain.Common;
using Pkm.Domain.Notifications;
using Pkm.Domain.Pages;
using Pkm.Domain.Recommendations;
using Pkm.Domain.Tasks;
using Pkm.Domain.Users;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence;

public sealed class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageRevision> PageRevisions => Set<PageRevision>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<BlockOperation> BlockOperations => Set<BlockOperation>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskAssignee> TaskAssignees => Set<TaskAssignee>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<TaskRecommendation> TaskRecommendations => Set<TaskRecommendation>();
    public DbSet<TaskPerformanceMetric> TaskPerformanceMetrics => Set<TaskPerformanceMetric>();
    public DbSet<UserTaskPreference> UserTaskPreferences => Set<UserTaskPreference>();
    public DbSet<UserTaskHistory> UserTaskHistories => Set<UserTaskHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model
            .GetEntityTypes()
            .Where(x =>
                x.ClrType is not null &&
                !x.IsOwned() &&
                typeof(EntityBase).IsAssignableFrom(x.ClrType));

        foreach (var entityType in entityTypes)
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");

            var isDeletedProperty = Expression.Property(
                Expression.Convert(parameter, typeof(EntityBase)),
                nameof(EntityBase.IsDeleted));

            var body = Expression.Equal(isDeletedProperty, Expression.Constant(false));
            var lambda = Expression.Lambda(body, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}