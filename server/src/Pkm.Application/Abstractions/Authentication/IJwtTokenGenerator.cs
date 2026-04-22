using Pkm.Domain.Users;

namespace Pkm.Application.Abstractions.Authentication;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, IEnumerable<string> roles);
    int GetAccessTokenExpirySeconds();
}
