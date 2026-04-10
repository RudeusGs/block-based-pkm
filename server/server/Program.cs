using server.Infrastructure.Cors;
using server.Infrastructure.Realtime;
using server.Infrastructure.Swagger;
using server.Service.Configurations;
namespace server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add Services
            builder.Services.AddControllers();

            // Add Swagger via extension
            builder.Services.AddCustomSwagger();

            // CORS Config
            builder.Services.AddCustomCors(builder.Configuration);
            // Custom DI
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddApplicationServices();
            
            // Build App
            var app = builder.Build();

            // Swagger UI
            app.UseCustomSwaggerUI();
            // Middleware
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();

            // Map Endpoints
            app.MapRealtimeHubs();
            app.MapControllers();

            app.Run();
        }
    }
}