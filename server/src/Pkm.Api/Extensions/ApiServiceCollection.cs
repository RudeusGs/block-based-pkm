using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Common.Errors;

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
        services.AddApiControllers();

        return services;
    }

    private static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services
            .AddControllers(options =>
            {
                options.Filters.Add(new ProducesAttribute("application/json"));
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = ApiModelStateResponseFactory.Create;
            });

        return services;
    }
}
