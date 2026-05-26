using Pkm.Api.Extensions;
using Pkm.Application;
using Pkm.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplicationServices()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseApiPipeline();
app.MapApiEndpoints();

app.Run();
