namespace Pkm.Infrastructure.Email;

public sealed class ApplicationUrlOptions
{
    public const string SectionName = "App";

    public string PublicBaseUrl { get; set; } = "http://localhost:5173";
    public string WorkspaceInvitationAcceptPath { get; set; } = "/success";
}
