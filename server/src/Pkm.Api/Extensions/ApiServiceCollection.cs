namespace Pkm.Api.Extensions;

public static class ApiServiceCollection
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddCustomCors(configuration, environment);
        services.AddCustomSwagger();
        services.AddControllers();

        return services;
    }
}
