namespace Pkm.Application.Abstractions.Email;

public interface IWorkspaceInvitationLinkFactory
{
    string CreateAcceptLink(string invitationToken);
}
