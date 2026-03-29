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

        /// <summary>
        /// Đảm bảo vai trò người dùng thường tồn tại, nếu không thì tạo mới.
        /// </summary>
        public async Task EnsureRegularRoleExistsAsync()
        {
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

        /// <summary>
        /// Gán vai trò người dùng thường cho người dùng.
        /// </summary>
        public async Task<(bool succeeded, List<string> errors)> AddToRegularRoleAsync(int userId)
        {
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