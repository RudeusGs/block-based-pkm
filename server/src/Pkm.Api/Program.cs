using Pkm.Api.Extensions;
using Pkm.Application;
using Pkm.Infrastructure.Authentication;
using Pkm.Infrastructure.Cache;
using Pkm.Infrastructure.Persistence;
using Pkm.Infrastructure.Realtime;
using Pkm.Infrastructure.Realtime.Hubs;
using Pkm.Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

// =========================
// Infrastructure
// =========================

// Persistence:
// - Đăng ký EF Core DataContext
// - Đăng ký repositories
// - Đăng ký UnitOfWork
builder.Services.AddPersistenceInfrastructure(builder.Configuration);

// Authentication:
// - Đăng ký JWT authentication
// - Đăng ký password hasher
// - Đăng ký current user / role service
builder.Services.AddAuthenticationInfrastructure(builder.Configuration);

// Cache:
// - Đăng ký Redis connection
// - Đăng ký Redis cache base
// - Đăng ký Redis key factory + serializer
builder.Services.AddCacheInfrastructure(builder.Configuration);

// Realtime:
// - Đăng ký SignalR hub
// - Đăng ký page presence service
// - Đăng ký block lease service
// - Đăng ký realtime publisher
builder.Services.AddRealtimeInfrastructure(builder.Configuration);

// Time:
// - Đăng ký SystemClock cho IClock
builder.Services.AddTimeInfrastructure();

// =========================
// Application
// =========================

// Application services:
// - Đăng ký handlers
// - Đăng ký validators
// - Đăng ký access evaluators
builder.Services.AddApplicationServices();

// =========================
// API
// =========================

// CORS:
// - Cho phép frontend gọi API theo danh sách origins trong config
builder.Services.AddCustomCors(builder.Configuration);

// Swagger/OpenAPI:
// - Sinh tài liệu API và UI cho môi trường dev
builder.Services.AddCustomSwagger();

// Controllers:
// - Đăng ký MVC controllers
builder.Services.AddControllers();

var app = builder.Build();

// =========================
// HTTP Pipeline
// =========================

// Global exception handling:
app.UseExceptionMapping();

// Swagger UI:
// - Chỉ bật ở môi trường Development theo logic trong extension
app.UseCustomSwaggerUI();

// HTTPS redirection:
// - Tự động chuyển HTTP sang HTTPS
app.UseHttpsRedirection();

// CORS:
// - Áp dụng policy cho frontend trước auth/authorization
app.UseCors(CorsServiceCollection.PolicyName);

// Authentication:
// - Xác thực người dùng từ JWT
app.UseAuthentication();

// Authorization:
// - Kiểm tra quyền sau khi xác thực thành công
app.UseAuthorization();

// =========================
// Endpoint Mapping
// =========================

// REST API controllers
app.MapControllers();

// SignalR hub cho collaboration / realtime
app.MapHub<CollaborationHub>("/hubs/collaboration");

app.Run();