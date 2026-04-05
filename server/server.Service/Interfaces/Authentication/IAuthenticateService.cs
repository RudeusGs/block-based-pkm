using server.Service.Models;
using server.Service.Models.Authenticate;

namespace server.Service.Interfaces
{
    public interface IAuthenticateService
    {
        /// <summary>
        /// Đăng nhập người dùng.
        /// </summary>
        Task<ApiResult> Login(LoginModel loginModel);

        /// <summary>
        /// Đăng ký người dùng mới.
        /// </summary>
        Task<ApiResult> Register(RegisterModel registerModel);

        /// <summary>
        /// Lấy vai trò của người dùng.
        /// </summary>
        Task<ApiResult> GetRoleUser(int id); 
    }
}