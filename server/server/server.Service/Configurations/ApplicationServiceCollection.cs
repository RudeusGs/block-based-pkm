using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using server.Domain.Entities;
using server.Service.Common.IServices;
using server.Service.Common.Services;
using server.Service.Interfaces;
using server.Service.Interfaces.Authentication;
using server.Service.Services;
using server.Service.Services.Authentication;

namespace server.Service.Configurations
{
    public static class ApplicationServiceCollection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            #region Common services

            // User Management Service
            // UserManager and SignInManager are registered by Identity in infrastructure services.
            services.AddScoped<IUserService, UserService>();

            #endregion

            services.AddHttpContextAccessor();

            #region Authentication services
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IRoleManagementService, RoleManagementService>();
            services.AddScoped<IAuthenticateService, AuthenticateService>();
            #endregion

            #region Business services

            #endregion

            return services;
        }
    }
}