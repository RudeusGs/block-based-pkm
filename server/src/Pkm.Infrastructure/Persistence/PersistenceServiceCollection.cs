using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Messaging;
using Pkm.Infrastructure.Persistence.Repositories;
using Pkm.Infrastructure.Persistence.Outbox;
using Pkm.Infrastructure.Persistence.Cleanup;

namespace Pkm.Infrastructure.Persistence;

public static class PersistenceServiceCollection
{
    private const string ConnectionStringKey = "ConnectionStrings:Connection";

    public static IServiceCollection AddPersistenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration[ConnectionStringKey]
            ?? throw new InvalidOperationException(
                $"Database connection string '{ConnectionStringKey}' is not configured.");

        services.AddDbContextPool<DataContext>(
            options => options.UseNpgsql(
                connectionString,
                npgsql => npgsql
                    .MigrationsAssembly(typeof(DataContext).Assembly.FullName)
                    .EnableRetryOnFailure()),
            poolSize: 256);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutboxMessageSerializer, SystemTextJsonOutboxMessageSerializer>();
        services.AddScoped<IOutboxDomainEventWriter, OutboxDomainEventWriter>();
        services.AddScoped<IIntegrationEventOutbox, EfCoreIntegrationEventOutbox>();
        services.AddScoped<IOutboxMessageDispatcher, OutboxMessageDispatcher>();
        services.AddScoped<IOutboxBatchProcessor, OutboxBatchProcessor>();
        services.AddHostedService<OutboxProcessorHostedService>();
        services.Configure<PageTrashCleanupOptions>(
            configuration.GetSection(PageTrashCleanupOptions.SectionName));
        services.AddHostedService<PageTrashCleanupHostedService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<MessagingRepository>();
        services.AddScoped<IMessagingReadRepository>(sp => sp.GetRequiredService<MessagingRepository>());
        services.AddScoped<IMessagingWriteRepository>(sp => sp.GetRequiredService<MessagingRepository>());
        services.AddScoped<IWorkspaceMemberRepository, WorkspaceMemberRepository>();
        services.AddScoped<IWorkspaceInvitationRepository, WorkspaceInvitationRepository>();
        services.AddScoped<PageRepository>();
        services.AddScoped<IPageReadRepository>(sp => sp.GetRequiredService<PageRepository>());
        services.AddScoped<IPageWriteRepository>(sp => sp.GetRequiredService<PageRepository>());
        services.AddScoped<IPageUserStateRepository, PageUserStateRepository>();
        services.AddScoped<BlockRepository>();
        services.AddScoped<IBlockReadRepository>(sp => sp.GetRequiredService<BlockRepository>());
        services.AddScoped<IBlockWriteRepository>(sp => sp.GetRequiredService<BlockRepository>());
        services.AddScoped<IPageRevisionRepository, PageRevisionRepository>();
        services.AddScoped<IBlockOperationRepository, BlockOperationRepository>();
        services.AddScoped<WorkTaskRepository>();
        services.AddScoped<IWorkTaskWriteRepository>(sp => sp.GetRequiredService<WorkTaskRepository>());
        services.AddScoped<IWorkTaskReadRepository>(sp => sp.GetRequiredService<WorkTaskRepository>());
        services.AddScoped<IWorkTaskRecommendationReadRepository>(sp => sp.GetRequiredService<WorkTaskRepository>());
        services.AddScoped<ITaskAssigneeRepository, TaskAssigneeRepository>();
        services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<ITaskRecommendationRepository, TaskRecommendationRepository>();
        services.AddScoped<IUserTaskPreferenceRepository, UserTaskPreferenceRepository>();
        services.AddScoped<IUserTaskHistoryRepository, UserTaskHistoryRepository>();
        services.AddScoped<IStoredFileRepository, StoredFileRepository>();

        return services;
    }
}



