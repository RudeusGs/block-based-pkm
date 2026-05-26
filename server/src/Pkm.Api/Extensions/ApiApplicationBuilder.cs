using Pkm.Infrastructure.Realtime.Hubs;

namespace Pkm.Api.Extensions;

public static class ApiApplicationBuilder
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseExceptionMapping();
        app.UseCustomSwaggerUI();
        app.UseHttpsRedirection();
        app.UseCors(CorsServiceCollection.PolicyName);
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
        endpoints.MapHub<CollaborationHub>(CollaborationHubRoute.Path);

        return endpoints;
    }
}
