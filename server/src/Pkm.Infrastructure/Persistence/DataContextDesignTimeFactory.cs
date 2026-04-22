using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Pkm.Infrastructure.Persistence;

public sealed class DataContextDesignTimeFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration["ConnectionStrings:Connection"] ??
            configuration.GetConnectionString("Connection") ??
            throw new InvalidOperationException(
                "Không tìm thấy connection string 'ConnectionStrings:Connection' hoặc GetConnectionString(\"Connection\").");

        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(DataContext).Assembly.FullName));

        return new DataContext(optionsBuilder.Options);
    }
}