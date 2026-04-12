using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using server.Domain.Constants;
using server.Domain.Entities;
using server.Service.Interfaces.Authentication;

namespace server.Service.Services.Authentication
{
    /// <summary>
    /// RoleManagementService: Quản lý vai trò của người dùng.
    /// </summary>
    public class RoleManagementService : IRoleManagementService
    {
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<RoleManagementService> _logger;

        public RoleManagementService(
            RoleManager<IdentityRole<int>> roleManager,
            UserManager<User> userManager,
            ILogger<RoleManagementService> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task EnsureRegularRoleExistsAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!await _roleManager.RoleExistsAsync(RoleConstants.REGULAR_USER))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole<int>(RoleConstants.REGULAR_USER));
                if (!result.Succeeded)
                {
                    var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Tạo vai trò '{RoleName}' thất bại. {Errors}", RoleConstants.REGULAR_USER, errors);
                    throw new InvalidOperationException($"Tạo vai trò '{RoleConstants.REGULAR_USER}' thất bại. {errors}");
                }
            }
        }

        public async Task EnsureRolesExistAsync(CancellationToken ct = default)
        {
            await EnsureRegularRoleExistsAsync(ct);

            ct.ThrowIfCancellationRequested();

            if (!await _roleManager.RoleExistsAsync(RoleConstants.ADMIN))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole<int>(RoleConstants.ADMIN));
                if (!result.Succeeded)
                {
                    var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Tạo vai trò '{RoleName}' thất bại. {Errors}", RoleConstants.ADMIN, errors);
                    throw new InvalidOperationException($"Tạo vai trò '{RoleConstants.ADMIN}' thất bại. {errors}");
                }
            }
        }

        public async Task<(bool succeeded, List<string> errors)> AddToRegularRoleAsync(int userId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return (false, new List<string> { "Người dùng không tồn tại." });

            var result = await _userManager.AddToRoleAsync(user, RoleConstants.REGULAR_USER);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return (false, errors);
            }

            return (true, new List<string>());
        }
    }
}
