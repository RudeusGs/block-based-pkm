using System.Security.Cryptography;
using System.Text;

namespace Pkm.Application.Features.Workspaces.Services;

public static class WorkspaceInvitationToken
{
    public static string Create()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string rawToken)
    {
        var normalized = rawToken.Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
