namespace Pkm.Infrastructure.Email;

public sealed class ApplicationUrlOptions
{
    public const string SectionName = "App";

    public string PublicBaseUrl { get; set; } = "https://localhost:7286";
    public string WorkspaceInvitationAcceptPath { get; set; } = "/api/v1/workspace-invitations/accept";
}
