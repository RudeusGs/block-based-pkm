using Microsoft.Extensions.Options;
using Pkm.Application.Abstractions.Email;

namespace Pkm.Infrastructure.Email;

internal sealed class WorkspaceInvitationLinkFactory : IWorkspaceInvitationLinkFactory
{
    private readonly ApplicationUrlOptions _options;

    public WorkspaceInvitationLinkFactory(IOptions<ApplicationUrlOptions> options)
    {
        _options = options.Value;
    }

    public string CreateAcceptLink(string invitationToken)
    {
        if (string.IsNullOrWhiteSpace(invitationToken))
            throw new ArgumentException("Invitation token không hợp lệ.", nameof(invitationToken));

        var baseUrl = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? "https://localhost:7286"
            : _options.PublicBaseUrl.TrimEnd('/');

        var path = string.IsNullOrWhiteSpace(_options.WorkspaceInvitationAcceptPath)
            ? "/success"
            : _options.WorkspaceInvitationAcceptPath;

        if (!path.StartsWith('/'))
            path = "/" + path;

        var builder = new UriBuilder(baseUrl + path)
        {
            Query = $"token={Uri.EscapeDataString(invitationToken)}"
        };

        return builder.Uri.ToString();
    }
}
