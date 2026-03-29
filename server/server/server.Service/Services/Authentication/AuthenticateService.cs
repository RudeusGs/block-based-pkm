using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using server.Domain.Entities;
using server.Service.Interfaces;
using server.Service.Interfaces.Authentication;
using server.Service.Models;
using server.Service.Models.Authenticate;

namespace server.Service.Services.Authentication
{
    /// <summary>
    /// AuthenticateService: Xử lý đăng nhập và đăng ký người dùng.
    /// </summary>
    public class AuthenticateService : IAuthenticateService
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRoleManagementService _roleManagementService;
        private readonly ILogger<AuthenticateService> _logger;

        public AuthenticateService(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            IJwtTokenService jwtTokenService,
            IRoleManagementService roleManagementService,
            ILogger<AuthenticateService> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _roleManagementService = roleManagementService;
            _logger = logger;
        }

        /// <summary>
        /// Đăng nhập người dùng.
        /// </summary>
        public async Task<ApiResult> Login(LoginModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Password))
                    return ApiResult.Fail("Tên đăng nhập hoặc mật khẩu không được để trống.", errorCode: "VALIDATION_ERROR");

                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user is null)
                    return ApiResult.Fail("Tên đăng nhập hoặc mật khẩu không chính xác.", errorCode: "INVALID_CREDENTIALS");

                var signIn = await _signInManager.PasswordSignInAsync(
                    model.UserName, model.Password, isPersistent: false, lockoutOnFailure: false);

                if (!signIn.Succeeded)
                    return ApiResult.Fail("Tên đăng nhập hoặc mật khẩu không chính xác.", errorCode: "INVALID_CREDENTIALS");

                var roles = await _userManager.GetRolesAsync(user);
                var token = _jwtTokenService.GenerateToken(user, roles);

                return ApiResult.Success(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed. Username={Username}", model?.UserName);
                return ApiResult.Fail("Có lỗi hệ thống khi đăng nhập. Vui lòng thử lại.", errorCode: "LOGIN_EXCEPTION");
            }
        }

        /// <summary>
        /// Đăng ký người dùng mới với vai trò người dùng thường.
        /// </summary>
        public async Task<ApiResult> Register(RegisterModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.UserName) ||
                    string.IsNullOrWhiteSpace(model.Email) ||
                    string.IsNullOrWhiteSpace(model.Password))
                {
                    return ApiResult.Fail("Tên đăng nhập, email hoặc mật khẩu không được để trống.", errorCode: "VALIDATION_ERROR");
                }

                if (await _userManager.FindByNameAsync(model.UserName) is not null)
                    return ApiResult.Fail($"Tên đăng nhập '{model.UserName}' đã tồn tại.", errorCode: "USERNAME_EXISTS");

                if (await _userManager.FindByEmailAsync(model.Email) is not null)
                    return ApiResult.Fail($"Email '{model.Email}' đã tồn tại.", errorCode: "EMAIL_EXISTS");

                var user = new User
                {
                    UserName = model.UserName.Trim(),
                    Email = model.Email.Trim(),
                    FullName = model.FullName?.Trim(),
                    CreatedDate = DateTime.Now,
                    IsAuthen = false
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (!createResult.Succeeded)
                {
                    var errors = createResult.Errors.Select(e => e.Description).ToList();
                    return ApiResult.Fail("Tạo tài khoản thất bại.", errorCode: "CREATE_USER_FAILED", errors: errors);
                }

                try
                {
                    await _roleManagementService.EnsureRegularRoleExistsAsync();
                    var (roleSucceeded, roleErrors) = await _roleManagementService.AddToRegularRoleAsync(user.Id);
                    if (!roleSucceeded)
                        return ApiResult.Fail("Gán vai trò người dùng thất bại.", errorCode: "ADD_ROLE_FAILED", errors: roleErrors);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Role management failed while registering user. Username={Username}", model?.UserName);
                    return ApiResult.Fail("Gán vai trò người dùng thất bại.", errorCode: "ADD_ROLE_FAILED", errors: new List<string> { ex.Message });
                }

                return ApiResult.Success(
                    data: new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.FullName
                    },
                    message: "Đăng ký thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register failed. Username={Username}, Email={Email}", model?.UserName, model?.Email);
                return ApiResult.Fail("Có lỗi hệ thống khi đăng ký. Vui lòng thử lại.", errorCode: "REGISTER_EXCEPTION");
            }
        }

        /// <summary>
        /// Lấy vai trò của người dùng.
        /// </summary>
        public async Task<ApiResult> GetRoleUser(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user is null)
                    return ApiResult.Fail("Người dùng không tồn tại.", errorCode: "USER_NOT_FOUND");

                var roles = await _userManager.GetRolesAsync(user);
                return ApiResult.Success(data: roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetRoleUser failed. UserId={UserId}", id);
                return ApiResult.Fail("Có lỗi hệ thống khi lấy vai trò. Vui lòng thử lại.", errorCode: "GET_ROLE_EXCEPTION");
            }
        }
    }
}
