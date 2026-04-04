using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace server.Infrastructure.Swagger
{
    public static class SwaggerApplicationBuilder
    {
        public static IApplicationBuilder UseCustomSwaggerUI(this IApplicationBuilder app)
        {
            if (app.ApplicationServices.GetService<IWebHostEnvironment>()?.IsDevelopment() ?? false)
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Block-Based PKM API v1");
                    options.RoutePrefix = "";
                    options.DocumentTitle = "Block-Based PKM API Docs";
                    options.DefaultModelsExpandDepth(-1);
                });
            }

            return app;
        }
    }
}