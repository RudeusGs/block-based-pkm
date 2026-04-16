using server.Service.Models;
using server.Service.Models.Authenticate;

namespace server.Service.Interfaces.Authentication
{
    public interface IAuthenticateService
    {
        Task<ApiResult> Login(LoginModel loginModel, CancellationToken ct = default);

        Task<ApiResult> Register(RegisterModel registerModel, CancellationToken ct = default);

        Task<ApiResult> GetRoleUser(int id, CancellationToken ct = default);
    }
}
