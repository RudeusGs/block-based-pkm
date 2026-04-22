using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Authentication;

namespace Pkm.Application.Features.Authentication.Queries.GetUserRoles;

public sealed class GetUserRolesHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleService _userRoleService;

    public GetUserRolesHandler(IUserRepository userRepository, IUserRoleService userRoleService)
    {
        _userRepository = userRepository;
        _userRoleService = userRoleService;
    }

    public async Task<Result<IEnumerable<string>>> HandleAsync(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result.Failure<IEnumerable<string>>(AuthenticationErrors.InvalidUserId);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<IEnumerable<string>>(AuthenticationErrors.UserNotFound);
        }

        var roles = await _userRoleService.GetUserRolesAsync(user.Id, cancellationToken);
        return Result.Success(roles);
    }
}
