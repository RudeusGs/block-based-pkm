using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Documents.Policies;
using Pkm.Application.Features.Documents.Services;
using Pkm.Application.Features.Files.Services;
using Pkm.Application.Features.Messaging.Services;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Application.Features.Recommendations.Services;
using Pkm.Application.Features.Social.Services;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Application;

public static class ApplicationServiceCollection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceCollection).Assembly;

        services.AddApplicationPolicies();
        services.AddApplicationServicesInternal();
        services.AddScoped<IUseCaseDispatcher, UseCaseDispatcher>();
        services.AddConcreteTypesBySuffix(applicationAssembly, "Handler", ServiceLifetime.Scoped);
        services.AddConcreteTypesBySuffix(applicationAssembly, "Validator", ServiceLifetime.Scoped);

        return services;
    }

    private static IServiceCollection AddApplicationPolicies(this IServiceCollection services)
    {
        services.AddScoped<IWorkspaceAccessEvaluator, WorkspaceAccessEvaluator>();
        services.AddScoped<IPageAccessEvaluator, PageAccessEvaluator>();
        services.AddScoped<IDocumentAccessEvaluator, DocumentAccessEvaluator>();
        services.AddScoped<ITaskAccessEvaluator, TaskAccessEvaluator>();

        return services;
    }

    private static IServiceCollection AddApplicationServicesInternal(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<IRecommendationScoringService, RecommendationScoringService>();
        services.AddScoped<IFileUploadApplicationService, FileUploadApplicationService>();
        services.AddScoped<SocialApplicationService>();
        services.AddScoped<ISocialApplicationService>(sp => sp.GetRequiredService<SocialApplicationService>());
        services.AddScoped<ISocialCommandService>(sp => sp.GetRequiredService<SocialApplicationService>());
        services.AddScoped<ISocialQueryService>(sp => sp.GetRequiredService<SocialApplicationService>());

        services.AddScoped<MessagingApplicationService>();
        services.AddScoped<IMessagingApplicationService>(sp => sp.GetRequiredService<MessagingApplicationService>());
        services.AddScoped<IMessagingCommandService>(sp => sp.GetRequiredService<MessagingApplicationService>());
        services.AddScoped<IMessagingQueryService>(sp => sp.GetRequiredService<MessagingApplicationService>());
        services.AddScoped<IBlockPayloadValidator, BlockPayloadValidator>();
        services.AddSingleton<IOrderKeyGenerator, LexicographicOrderKeyGenerator>();

        return services;
    }

    private static IServiceCollection AddConcreteTypesBySuffix(
        this IServiceCollection services,
        Assembly assembly,
        string suffix,
        ServiceLifetime lifetime)
    {
        var implementationTypes = assembly
            .DefinedTypes
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                type.Name.EndsWith(suffix, StringComparison.Ordinal) &&
                type.DeclaredConstructors.Any(ctor => !ctor.IsStatic))
            .Select(type => type.AsType());

        foreach (var implementationType in implementationTypes)
        {
            services.Add(new ServiceDescriptor(
                implementationType,
                implementationType,
                lifetime));

            foreach (var serviceType in GetUseCaseHandlerInterfaces(implementationType))
            {
                services.Add(new ServiceDescriptor(
                    serviceType,
                    implementationType,
                    lifetime));
            }
        }

        return services;
    }

    private static IEnumerable<Type> GetUseCaseHandlerInterfaces(Type implementationType)
        => implementationType
            .GetInterfaces()
            .Where(type =>
                type.IsGenericType &&
                type.GetGenericTypeDefinition() is var genericDefinition &&
                (genericDefinition == typeof(ICommandHandler<>) ||
                 genericDefinition == typeof(ICommandHandler<,>) ||
                 genericDefinition == typeof(IQueryHandler<,>)));
}
