using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pkm.Application.Common.Abstractions.Storage;

namespace Pkm.Infrastructure.Storage;

public static class StorageServiceCollection
{
    public static IServiceCollection AddStorageInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CloudinaryStorageOptions>(
            configuration.GetSection(CloudinaryStorageOptions.SectionName));

        services.AddSingleton<IFileStorageService, CloudinaryFileStorageService>();

        return services;
    }
}
