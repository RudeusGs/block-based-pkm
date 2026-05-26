using Pkm.Api.Extensions;
using Pkm.Application;
using Pkm.Infrastructure.Authentication;
using Pkm.Infrastructure.Cache;
using Pkm.Infrastructure.Persistence;
using Pkm.Infrastructure.Email;
using Pkm.Infrastructure.Realtime;
using Pkm.Infrastructure.Realtime.Hubs;
using Pkm.Infrastructure.Time;
using Pkm.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddAuthenticationInfrastructure(builder.Configuration);
builder.Services.AddCacheInfrastructure(builder.Configuration);
builder.Services.AddEmailInfrastructure(builder.Configuration);
builder.Services.AddRealtimeInfrastructure(builder.Configuration);
builder.Services.AddTimeInfrastructure();
builder.Services.AddStorageInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddCustomCors(builder.Configuration, builder.Environment);
builder.Services.AddCustomSwagger();
builder.Services.AddControllers();

var app = builder.Build();

app.UseExceptionMapping();
app.UseCustomSwaggerUI();
app.UseHttpsRedirection();
app.UseCors(CorsServiceCollection.PolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<CollaborationHub>("/hubs/collaboration");

app.Run();
