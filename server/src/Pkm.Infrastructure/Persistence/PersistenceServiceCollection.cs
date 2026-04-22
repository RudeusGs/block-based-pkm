using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Infrastructure.Persistence.Repositories;

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

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IWorkspaceMemberRepository, WorkspaceMemberRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IBlockRepository, BlockRepository>();
        services.AddScoped<IPageRevisionRepository, PageRevisionRepository>();
        services.AddScoped<IBlockOperationRepository, BlockOperationRepository>();
        services.AddScoped<IWorkTaskRepository, WorkTaskRepository>();
        services.AddScoped<ITaskAssigneeRepository, TaskAssigneeRepository>();

        return services;
    }
}