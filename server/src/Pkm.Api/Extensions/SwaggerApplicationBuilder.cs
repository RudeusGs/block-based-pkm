namespace Pkm.Api.Extensions;

public static class SwaggerApplicationBuilder
{
    public static IApplicationBuilder UseCustomSwaggerUI(this IApplicationBuilder app)
    {
        var environment = app.ApplicationServices.GetRequiredService<IHostEnvironment>();

        if (!environment.IsDevelopment())
            return app;

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Block-Based PKM API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Block-Based PKM API Docs";
            options.DefaultModelsExpandDepth(-1);
            options.DisplayRequestDuration();
        });

        return app;
    }
}