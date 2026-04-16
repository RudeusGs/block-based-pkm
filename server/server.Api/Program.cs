using server.Infrastructure;
using server.Infrastructure.Cors;
using server.Infrastructure.Persistence;
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

            // Persistence (EF Core only — safe for design-time tools and migrations)
            builder.Services.AddPersistenceLayer(builder.Configuration);
            // Identity + JWT (depends on DbContext, not on Redis/SignalR)
            builder.Services.AddCoreInfrastructure(builder.Configuration);
            // Application / business services
            builder.Services.AddApplicationServices();
            // Optional: Redis backplane, cache, presence; falls back to in-memory when Redis is unavailable
            builder.Services.AddRealtimeInfrastructure(builder.Configuration);
            
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