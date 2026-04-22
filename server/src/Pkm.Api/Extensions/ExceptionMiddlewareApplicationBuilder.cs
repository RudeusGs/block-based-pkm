using Pkm.Api.Middleware;

namespace Pkm.Api.Extensions;

public static class ExceptionMiddlewareApplicationBuilder
{
    public static IApplicationBuilder UseExceptionMapping(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMappingMiddleware>();
    }
}